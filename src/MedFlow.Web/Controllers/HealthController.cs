using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MedFlow.Web.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheck;

    public HealthController(HealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }

    /// <summary>
    /// Liveness probe: confirma que el proceso está vivo.
    /// Sin autenticación — seguro para Kubernetes/load balancers.
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live() => Ok(new { status = "Alive", timestamp = DateTime.UtcNow });

    /// <summary>
    /// Readiness probe: verifica que todas las dependencias están disponibles.
    /// No expone nombres ni detalles de componentes internos para evitar reconocimiento.
    /// </summary>
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = await _healthCheck.CheckHealthAsync(ct);
        var isHealthy = result.Status == HealthStatus.Healthy;
        var statusCode = isHealthy ? 200 : 503;

        // Solo expone el estado global y la duración total.
        // No expone nombres de checks individuales ni descripciones.
        var response = new
        {
            status = isHealthy ? "Healthy" : "Unhealthy",
            totalDurationMs = (int)result.TotalDuration.TotalMilliseconds
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Endpoint de diagnóstico detallado — solo para SuperAdmin autenticado.
    /// </summary>
    [HttpGet("details")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Details(CancellationToken ct)
    {
        var result = await _healthCheck.CheckHealthAsync(ct);
        var statusCode = result.Status == HealthStatus.Healthy ? 200 : 503;

        var response = new
        {
            status = result.Status.ToString(),
            totalDurationMs = (int)result.TotalDuration.TotalMilliseconds,
            checks = result.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = (int)e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description
            })
        };

        return StatusCode(statusCode, response);
    }
}
