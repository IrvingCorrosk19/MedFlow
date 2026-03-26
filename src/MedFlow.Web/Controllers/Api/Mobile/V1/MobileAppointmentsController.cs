using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/appointments")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class MobileAppointmentsController : ControllerBase
{
    private readonly IPatientPortalService _portal;

    public MobileAppointmentsController(IPatientPortalService portal)
    {
        _portal = portal;
    }

    private async Task<Guid?> GetPatientIdAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _portal.GetPatientIdByUserIdAsync(userId, ct);
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientAppointmentListItemDto>), 200)]
    public async Task<IActionResult> GetUpcoming(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var list = await _portal.GetUpcomingAppointmentsAsync(patientId.Value, ct);
        return Ok(list);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientAppointmentListItemDto>), 200)]
    public async Task<IActionResult> GetHistory([FromQuery] int take = 50, CancellationToken ct = default)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var list = await _portal.GetAppointmentHistoryAsync(patientId.Value, take, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientAppointmentListItemDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var apt = await _portal.GetAppointmentAsync(patientId.Value, id, ct);
        if (apt == null)
            return NotFound();

        return Ok(apt);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var ok = await _portal.ConfirmAppointmentAsync(patientId.Value, id, ct);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest? body, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var ok = await _portal.CancelAppointmentAsync(patientId.Value, id, body?.Reason, ct);
        return ok ? Ok(new { success = true }) : NotFound();
    }
}

public record CancelAppointmentRequest(string? Reason);
