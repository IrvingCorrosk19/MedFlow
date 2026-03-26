using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api;

[ApiController]
[Route("api/analytics")]
[Authorize]
[RequirePermission(PermissionCodes.ReportsView)]
public class AnalyticsExportController : ControllerBase
{
    private readonly IAdvancedAnalyticsService _analytics;
    private readonly ITenantContext _tenant;

    public AnalyticsExportController(IAdvancedAnalyticsService analytics, ITenantContext tenant)
    {
        _analytics = analytics;
        _tenant = tenant;
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 1, 365));
        var filter = new AdvancedAnalyticsFilter(_tenant.TenantId, fromDate, toDate, 365);

        var data = await _analytics.GetDailySnapshotsAsync(filter, ct);
        return Ok(data);
    }

    [HttpGet("trends/appointments")]
    public async Task<IActionResult> GetAppointmentsTrend([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 1, 365));
        var filter = new AdvancedAnalyticsFilter(_tenant.TenantId, fromDate, toDate, 365);

        var data = await _analytics.GetAppointmentsTrendAsync(filter, ct);
        return Ok(data);
    }

    [HttpGet("trends/revenue")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 1, 365));
        var filter = new AdvancedAnalyticsFilter(_tenant.TenantId, fromDate, toDate, 365);

        var data = await _analytics.GetRevenueTrendAsync(filter, ct);
        return Ok(data);
    }
}
