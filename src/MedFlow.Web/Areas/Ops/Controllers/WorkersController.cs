using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.Ops.Controllers;

[Area("Ops")]
[Route("Ops/[controller]")]
[Authorize(Roles = "SuperAdmin")]
[ApiController]
public class WorkersController : ControllerBase
{
    private readonly IWorkerHeartbeatService _heartbeat;

    public WorkersController(IWorkerHeartbeatService heartbeat)
    {
        _heartbeat = heartbeat;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var workers = await _heartbeat.GetRecentAsync(ct);
        return Ok(workers.Select(w => new
        {
            w.WorkerName,
            w.LastSeenAt,
            w.Status,
            w.Details,
            stale = (DateTime.UtcNow - w.LastSeenAt).TotalMinutes > 5
        }));
    }
}
