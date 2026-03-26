using MedFlow.Application.Options;
using MedFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedFlow.Infrastructure.Startup;

public sealed class StartupValidator : IStartupValidator
{
    // Valores genéricos que son inseguros en CUALQUIER entorno.
    private static readonly HashSet<string> KnownPlaceholderJwtSecrets = new(StringComparer.OrdinalIgnoreCase)
    {
        "ChangeThisInProduction_UseAtLeast32Characters_OrSetJwt__Secret",
        "your-secret-here",
        "secret",
        "changeme"
    };

    // Secrets de desarrollo: válidos en Development, bloqueados en cualquier otro entorno.
    private static readonly HashSet<string> KnownDevOnlyJwtSecrets = new(StringComparer.OrdinalIgnoreCase)
    {
        "dev-only-secret-min-32-chars-change-for-prod!"
    };

    private static readonly HashSet<string> KnownPlaceholderN8nKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "medflow-dev-n8n-key-change-in-production",
        "dev-n8n-local-key",
        "changeme",
        "your-key-here"
    };

    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public StartupValidator(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public async Task<StartupValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var criticalErrors = new List<string>();

        // ── Conexión a la base de datos ──────────────────────────────────────
        var connStr = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            criticalErrors.Add("ConnectionStrings:DefaultConnection es requerida.");
        }
        else if (connStr.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase) && !IsDevelopment())
        {
            criticalErrors.Add("La conexión a la base de datos usa la contraseña por defecto 'postgres'. " +
                               "Establece ConnectionStrings__DefaultConnection vía variable de entorno.");
        }

        // ── JWT Secret ───────────────────────────────────────────────────────
        var jwtSecret = _configuration[$"{JwtOptions.SectionName}:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            criticalErrors.Add("Jwt:Secret es requerido para la autenticación de la API móvil.");
        }
        else
        {
            if (KnownPlaceholderJwtSecrets.Contains(jwtSecret))
                criticalErrors.Add("Jwt:Secret contiene un valor placeholder inseguro. " +
                                   "Establece Jwt__Secret vía variable de entorno con un secreto seguro.");
            else if (KnownDevOnlyJwtSecrets.Contains(jwtSecret) && !IsDevelopment())
                criticalErrors.Add("Jwt:Secret contiene el secret de desarrollo. " +
                                   "Establece Jwt__Secret con un valor seguro exclusivo para este entorno.");
            else if (jwtSecret.Length < 32 && IsProduction())
                criticalErrors.Add("Jwt:Secret debe tener al menos 32 caracteres en producción.");
        }

        // ── Clave API de n8n ─────────────────────────────────────────────────
        var n8nKey = _configuration["Integrations:N8n:ApiKey"];
        if (!string.IsNullOrWhiteSpace(n8nKey) && KnownPlaceholderN8nKeys.Contains(n8nKey) && IsProduction())
        {
            criticalErrors.Add("Integrations:N8n:ApiKey contiene un valor placeholder. " +
                               "Establece una clave segura vía variable de entorno.");
        }

        // ── CORS en producción ───────────────────────────────────────────────
        var allowAnyOrigin = _configuration.GetValue<bool>("Cors:AllowAnyOrigin");
        if (allowAnyOrigin && IsProduction())
        {
            criticalErrors.Add("Cors:AllowAnyOrigin está activado en producción. " +
                               "Define orígenes específicos en Cors:AllowedOrigins.");
        }

        // ── AllowedHosts en producción ───────────────────────────────────────
        var allowedHosts = _configuration["AllowedHosts"];
        if (allowedHosts == "*" && IsProduction())
        {
            errors.Add("AllowedHosts está configurado como '*' en producción. " +
                       "Especifica los hostnames permitidos para evitar host header injection.");
        }

        // ── Conectividad a la base de datos ──────────────────────────────────
        if (criticalErrors.Count == 0) // Solo si las validaciones previas pasan
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _ = await db.Database.CanConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                criticalErrors.Add($"Fallo de conexión a la base de datos: {ex.Message}");
            }
        }

        var allErrors = criticalErrors.Concat(errors).ToList();
        return new StartupValidationResult(allErrors.Count == 0, allErrors, criticalErrors);
    }

    private bool IsDevelopment() =>
        string.Equals(_configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);

    private bool IsProduction() =>
        !string.Equals(_configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
}
