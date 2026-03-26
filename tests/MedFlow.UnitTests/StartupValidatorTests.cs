using MedFlow.Infrastructure.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MedFlow.Infrastructure.Persistence;
using MedFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Tests for StartupValidator configuration validation logic.
/// The DB connectivity check is bypassed by providing an InMemory context that can connect.
/// </summary>
public class StartupValidatorTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IServiceProvider BuildServices(IConfiguration config)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        tenant.Setup(t => t.IgnoreTenantFilter).Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseInMemoryDatabase("startup-validator-test"));
        services.AddSingleton(tenant.Object);
        return services.BuildServiceProvider();
    }

    private static StartupValidator CreateValidator(Dictionary<string, string?> configValues)
    {
        var config = BuildConfig(configValues);
        var sp = BuildServices(config);
        return new StartupValidator(sp, config);
    }

    // ── JWT Secret ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_PlaceholderJwtSecret_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=secure",
            ["Jwt:Secret"] = "ChangeThisInProduction_UseAtLeast32Characters_OrSetJwt__Secret"
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("placeholder"));
    }

    [Fact]
    public async Task Validate_ShortJwtSecret_InProduction_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=secure",
            ["Jwt:Secret"] = "short"
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("32 caracteres"));
    }

    [Fact]
    public async Task Validate_ShortJwtSecret_InDevelopment_IsNotCritical()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=postgres",
            ["Jwt:Secret"] = "short"
        });

        var result = await v.ValidateAsync();

        // In Development mode, short JWT and default postgres password are allowed
        Assert.False(result.HasCriticalErrors);
    }

    [Fact]
    public async Task Validate_EmptyJwtSecret_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=secure",
            ["Jwt:Secret"] = ""
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("Jwt:Secret"));
    }

    // ── N8n API Key ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_PlaceholderN8nKey_InProduction_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=secure",
            ["Jwt:Secret"] = "a-valid-secret-that-is-at-least-32-chars-long!",
            ["Integrations:N8n:ApiKey"] = "medflow-dev-n8n-key-change-in-production"
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("N8n:ApiKey"));
    }

    [Fact]
    public async Task Validate_PlaceholderN8nKey_InDevelopment_IsNotCritical()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow",
            ["Jwt:Secret"] = "dev-only-secret-min-32-chars-change-for-prod!",
            ["Integrations:N8n:ApiKey"] = "medflow-dev-n8n-key-change-in-production"
        });

        var result = await v.ValidateAsync();

        Assert.False(result.HasCriticalErrors);
    }

    // ── CORS ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_AllowAnyOrigin_InProduction_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=secure",
            ["Jwt:Secret"] = "a-valid-secret-that-is-at-least-32-chars-long!",
            ["Cors:AllowAnyOrigin"] = "true"
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("AllowAnyOrigin"));
    }

    // ── Missing connection string ──────────────────────────────────────────

    [Fact]
    public async Task Validate_MissingConnectionString_IsCriticalError()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Jwt:Secret"] = "a-valid-secret-that-is-at-least-32-chars-long!"
        });

        var result = await v.ValidateAsync();

        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.CriticalErrors!, e => e.Contains("DefaultConnection"));
    }

    // ── Valid configuration ────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ValidProductionConfig_HasNoCriticalErrors()
    {
        var v = CreateValidator(new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=medflow;Password=Str0ngP@ss!",
            ["Jwt:Secret"] = "a-valid-secret-that-is-at-least-32-chars-long!",
            ["Cors:AllowAnyOrigin"] = "false",
            ["AllowedHosts"] = "app.medflow.ai"
        });

        var result = await v.ValidateAsync();

        Assert.False(result.HasCriticalErrors);
    }
}
