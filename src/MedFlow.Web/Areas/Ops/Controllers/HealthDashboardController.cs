using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MedFlow.Web.Areas.Ops.Controllers;

[Area("Ops")]
[Route("Ops/[controller]")]
[Authorize(Roles = "SuperAdmin")]
[ApiController]
public class HealthDashboardController : ControllerBase
{
    private readonly HealthCheckService _healthCheck;

    public HealthDashboardController(HealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _healthCheck.CheckHealthAsync(ct);
        return Ok(new
        {
            status = result.Status.ToString(),
            totalDuration = result.TotalDuration.TotalMilliseconds,
            checks = result.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
    }
}
