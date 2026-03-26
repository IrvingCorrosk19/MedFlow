using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MedFlow.Web.Areas.Ops.Controllers;

[Area("Ops")]
[Authorize(Roles = "SuperAdmin")]
public class HomeController : Controller
{
    private readonly HealthCheckService _healthCheck;
    private readonly IWorkerHeartbeatService _heartbeat;

    public HomeController(HealthCheckService healthCheck, IWorkerHeartbeatService heartbeat)
    {
        _healthCheck = healthCheck;
        _heartbeat = heartbeat;
    }

    [Route("Ops")]
    [Route("Ops/Home")]
    [Route("Ops/Home/Index")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var health = await _healthCheck.CheckHealthAsync(ct);
        var workers = await _heartbeat.GetRecentAsync(ct);

        ViewData["Title"] = "Operaciones";
        ViewData["HealthStatus"] = health.Status.ToString();
        ViewData["HealthDuration"] = health.TotalDuration.TotalMilliseconds;
        ViewData["Workers"] = workers;
        ViewData["HealthEntries"] = health.Entries.Select(e => new HealthEntryVm(e.Key, e.Value.Status.ToString())).ToList();

        return View();
    }
}
