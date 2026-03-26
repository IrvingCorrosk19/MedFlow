using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware>? _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware>? logger = null)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        if (tenantContext is not TenantContext tc)
        {
            await _next(context);
            return;
        }

        tc.SetIgnoreTenantFilter(false);

        var tenant = await ResolveTenantAsync(context, db, configuration, cancellationToken: context.RequestAborted);
        if (tenant == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("No hay tenant configurado o el código no existe. Ejecute migraciones y seed (TenantResolution:DefaultTenantCode).", context.RequestAborted);
            return;
        }

        tc.SetResolvedTenant(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.SecondaryColor);

        // Sin esto, el filtro global excluye usuarios con TenantId null (SuperAdmin) en login.
        if (context.User.Identity?.IsAuthenticated != true && IsAnonymousAuthPath(context.Request.Path))
        {
            tc.SetIgnoreTenantFilter(true);
            _logger?.LogDebug("Tenant: IgnoreTenantFilter=true para ruta auth anónima {Path}", context.Request.Path);
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            // El filtro global de ApplicationUser oculta TenantId=null salvo con IgnoreTenantFilter;
            // hace falta para resolver SuperAdmin (usuario de plataforma).
            tc.SetIgnoreTenantFilter(true);
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var super = await userManager.IsInRoleAsync(user, "SuperAdmin");
                if (super)
                {
                    tc.SetIgnoreTenantFilter(true);
                }
                else
                {
                    tc.SetIgnoreTenantFilter(false);
                    if (user.TenantId == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Usuario sin tenant asignado.");
                        return;
                    }

                    if (user.TenantId.Value != tc.TenantId!.Value)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("El tenant de la sesión no coincide con el contexto solicitado.");
                        return;
                    }
                }
            }
            else
            {
                tc.SetIgnoreTenantFilter(false);
            }
        }

        await _next(context);
    }

    private static bool IsAnonymousAuthPath(PathString path)
    {
        var p = path.Value ?? "";
        if (p.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase))
            return true;
        if (p.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase))
            return true;
        if (p.StartsWith("/PatientPortal/login", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static async Task<Tenant?> ResolveTenantAsync(
        HttpContext context,
        ApplicationDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var defaultCode = configuration["TenantResolution:DefaultTenantCode"] ?? "demo";
        var allowHeader = configuration.GetValue("TenantResolution:AllowHeaderOverride", true);

        Guid? headerId = null;
        string? headerCode = null;
        if (allowHeader)
        {
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var hv))
            {
                var raw = hv.ToString();
                if (Guid.TryParse(raw, out var gid))
                    headerId = gid;
            }
            if (!headerId.HasValue && context.Request.Headers.TryGetValue("X-Tenant-Code", out var hc))
                headerCode = hc.ToString()?.Trim();
        }

        if (headerCode != null)
        {
            var byCode = await db.Tenants.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == headerCode && !t.IsDeleted, cancellationToken);
            if (byCode != null)
                return byCode;
        }

        if (headerId.HasValue)
        {
            var byId = await db.Tenants.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == headerId.Value && !t.IsDeleted, cancellationToken);
            if (byId != null)
                return byId;
        }

        var host = context.Request.Host.Host;
        var code = ExtractSubdomainCode(host, configuration);

        if (string.IsNullOrEmpty(code))
            code = defaultCode;

        var tenant = await db.Tenants.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == code && !t.IsDeleted, cancellationToken);

        if (tenant == null && !string.Equals(code, defaultCode, StringComparison.OrdinalIgnoreCase))
        {
            tenant = await db.Tenants.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == defaultCode && !t.IsDeleted, cancellationToken);
        }

        return tenant;
    }

    private static string? ExtractSubdomainCode(string host, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var suffix = configuration["TenantResolution:HostSuffix"];
        if (!string.IsNullOrEmpty(suffix) && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = host[..^suffix.Length].TrimEnd('.');
            var parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1)
                return NormalizeCode(parts[0]);
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return null;

        var segs = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length >= 3)
            return NormalizeCode(segs[0]);

        return null;
    }

    private static string NormalizeCode(string s) =>
        s.Trim().ToLowerInvariant().Replace(' ', '-');
}
