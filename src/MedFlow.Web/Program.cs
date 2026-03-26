using System.Text;
using MedFlow.Application.Options;
using Microsoft.AspNetCore.Http;
using MedFlow.Infrastructure;
using MedFlow.Infrastructure.Health;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using MedFlow.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Npgsql: permite DateTime con Kind=Local/Unspecified; los convierte a UTC para timestamptz
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

static SameSiteMode ParseSameSite(string? value) => value?.ToLowerInvariant() switch
{
    "strict" => SameSiteMode.Strict,
    "none" => SameSiteMode.None,
    _ => SameSiteMode.Lax
};

builder.Services.Configure<SaasOptions>(builder.Configuration.GetSection(SaasOptions.SectionName));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection(ObservabilityOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<OnboardingOptions>(builder.Configuration.GetSection(OnboardingOptions.SectionName));
builder.Services.Configure<StripeBillingOptions>(builder.Configuration.GetSection(StripeBillingOptions.SectionName));
builder.Services.Configure<BillingDunningOptions>(builder.Configuration.GetSection(BillingDunningOptions.SectionName));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(45);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".MedFlow.Onboarding";
    options.Cookie.SameSite = ParseSameSite(builder.Configuration[$"{SecurityOptions.SectionName}:CookieSameSite"] ?? "Lax");
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtSecret = builder.Configuration[$"{JwtOptions.SectionName}:Secret"] ?? jwtOptions.Secret;
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication()
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });
}

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = corsOptions.AllowedOrigins ?? [];
        if (corsOptions.AllowAnyOrigin || origins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(origins)
                .WithMethods(corsOptions.AllowedMethods ?? ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"])
                .WithHeaders(corsOptions.AllowedHeaders ?? ["Authorization", "Content-Type", "X-Correlation-ID", "X-Tenant-Code", "X-Api-Key", "X-N8n-Api-Key"]);
            if (corsOptions.AllowCredentials)
                policy.AllowCredentials();
        }
    });
});

builder.Services.AddHealthChecks().AddMedFlowHealthChecks(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Soporte para HTTPS real detrás de reverse proxy (nginx, Traefik, AWS ALB).
// Necesario para que HSTS y Request.IsHttps funcionen correctamente.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<MedFlow.Infrastructure.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<MedFlow.Infrastructure.Middleware.GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<MedFlow.Infrastructure.Middleware.SecurityHeadersMiddleware>();
app.UseMiddleware<MedFlow.Infrastructure.Middleware.RateLimitingMiddleware>();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<MedFlow.Infrastructure.Middleware.RequestLoggingMiddleware>();
app.UseSession();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantCommercialMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllers();
app.MapRazorPages();
// Startup probe para Kubernetes — respuesta sanitizada: sin nombres de componentes.
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = static _ => true,
    ResponseWriter = static async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var healthy = report.Status == HealthStatus.Healthy;
        await ctx.Response.WriteAsync(
            $"{{\"status\":\"{(healthy ? "Healthy" : "Unhealthy")}\",\"totalDurationMs\":{(int)report.TotalDuration.TotalMilliseconds}}}");
    }
});

using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // ── Validación de configuración al arranque ───────────────────────────
    var validator = scope.ServiceProvider.GetRequiredService<MedFlow.Infrastructure.Startup.IStartupValidator>();
    var validation = await validator.ValidateAsync();

    foreach (var err in validation.Errors)
        startupLogger.LogWarning("Startup validation: {Error}", err);

    if (validation.HasCriticalErrors)
    {
        startupLogger.LogCritical(
            "Se detectaron {Count} error(es) crítico(s) de configuración. La aplicación no puede arrancar de forma segura. " +
            "Revisa los errores anteriores y corrige la configuración antes de reiniciar.",
            validation.CriticalErrors!.Count);
        await app.StopAsync();
        Environment.Exit(1);
        return;
    }

    // ── Migraciones de base de datos ──────────────────────────────────────
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Count != 0)
    {
        startupLogger.LogInformation(
            "Aplicando {Count} migración(es) pendiente(s): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));
        await db.Database.MigrateAsync();
        startupLogger.LogInformation("Migraciones aplicadas correctamente.");
    }

    await MedFlow.Infrastructure.Persistence.DataSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
        startupLogger);

    // Solo desarrollo: usuarios QA por rol (tenant demo) — ver Development:QaRoleUsersPassword.
    if (app.Environment.IsDevelopment())
    {
        var qaPwd = app.Configuration["Development:QaRoleUsersPassword"];
        if (!string.IsNullOrWhiteSpace(qaPwd))
        {
            await MedFlow.Infrastructure.Persistence.DataSeeder.SeedQaTenantRoleUsersAsync(
                db,
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
                qaPwd,
                startupLogger);
        }
    }

    // Solo desarrollo: restablecer admin de clínica si Development:ApplyAdminPassword tiene valor.
    if (app.Environment.IsDevelopment())
    {
        var devAdminPwd = app.Configuration["Development:ApplyAdminPassword"];
        if (!string.IsNullOrWhiteSpace(devAdminPwd))
        {
            var tenantCtx2 = scope.ServiceProvider.GetRequiredService<MedFlow.Application.Interfaces.ITenantContext>();
            tenantCtx2.SetIgnoreTenantFilter(true);
            var um2 = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var u2 = await um2.FindByEmailAsync("admin@medflow.ai");
            if (u2 != null)
            {
                var token2 = await um2.GeneratePasswordResetTokenAsync(u2);
                var r2 = await um2.ResetPasswordAsync(u2, token2, devAdminPwd);
                if (r2.Succeeded)
                {
                    await um2.SetLockoutEndDateAsync(u2, null);
                    await um2.ResetAccessFailedCountAsync(u2);
                    startupLogger.LogWarning("Development: contraseña de admin@medflow.ai actualizada.");
                }
            }
        }
    }

    // Solo desarrollo: restablecer superadmin si Development:ApplySuperAdminPassword tiene valor (user-secrets o env).
    if (app.Environment.IsDevelopment())
    {
        var devPwd = app.Configuration["Development:ApplySuperAdminPassword"];
        if (!string.IsNullOrWhiteSpace(devPwd))
        {
            var tenantCtx = scope.ServiceProvider.GetRequiredService<MedFlow.Application.Interfaces.ITenantContext>();
            tenantCtx.SetIgnoreTenantFilter(true);

            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var u = await um.FindByEmailAsync("superadmin@medflow.ai");
            if (u != null)
            {
                startupLogger.LogInformation("Development: superadmin encontrado, aplicando contraseña…");
                var token = await um.GeneratePasswordResetTokenAsync(u);
                var r = await um.ResetPasswordAsync(u, token, devPwd);
                if (r.Succeeded)
                {
                    await um.SetLockoutEndDateAsync(u, null);
                    await um.ResetAccessFailedCountAsync(u);
                    startupLogger.LogWarning(
                        "Development: contraseña de superadmin@medflow.ai actualizada y cuenta desbloqueada.");
                }
                else
                {
                    startupLogger.LogError("Development: no se pudo aplicar contraseña a superadmin: {Errors}",
                        string.Join(", ", r.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                startupLogger.LogWarning("Development: superadmin@medflow.ai no encontrado en DB. Ejecuta migraciones y seed.");
            }
        }
    }

    // Solo desarrollo: restablecer TODOS los usuarios si Development:ApplyAllUsersPassword tiene valor.
    if (app.Environment.IsDevelopment())
    {
        var allPwd = app.Configuration["Development:ApplyAllUsersPassword"];
        if (!string.IsNullOrWhiteSpace(allPwd))
        {
            var tenantCtx = scope.ServiceProvider.GetRequiredService<MedFlow.Application.Interfaces.ITenantContext>();
            tenantCtx.SetIgnoreTenantFilter(true);

            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var users = await um.Users.ToListAsync();
            var ok = 0;
            var fail = 0;

            foreach (var u in users)
            {
                var token = await um.GeneratePasswordResetTokenAsync(u);
                var r = await um.ResetPasswordAsync(u, token, allPwd);
                if (r.Succeeded)
                {
                    await um.SetLockoutEndDateAsync(u, null);
                    await um.ResetAccessFailedCountAsync(u);
                    ok++;
                }
                else
                {
                    fail++;
                    startupLogger.LogError(
                        "Development: no se pudo aplicar contraseña a {Email}: {Errors}",
                        u.Email ?? u.UserName ?? u.Id,
                        string.Join(", ", r.Errors.Select(e => e.Description)));
                }
            }

            startupLogger.LogWarning(
                "Development: contraseña aplicada a TODOS los usuarios. OK={Ok}, FAIL={Fail}",
                ok, fail);
        }
    }
}

app.Run();
