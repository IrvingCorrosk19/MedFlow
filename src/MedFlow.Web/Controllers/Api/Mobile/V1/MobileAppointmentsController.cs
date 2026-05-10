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

    /// <summary>Request a new appointment from the mobile app.</summary>
    [HttpPost("request")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RequestAppointment([FromBody] MobileAppointmentRequestBody body, CancellationToken ct)
    {
        if (body is null)
            return BadRequest(new { error = "Request body is required." });

        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        if (!DateTime.TryParse(body.DesiredDate, out var date))
            return BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });

        if (!TimeSpan.TryParse(body.StartTime, out var start) || !TimeSpan.TryParse(body.EndTime, out var end))
            return BadRequest(new { error = "Invalid time format. Use HH:mm." });

        var dto = new PortalAppointmentRequestDto(body.DoctorId, date, start, end, body.Reason);
        var (ok, err) = await _portal.RequestAppointmentAsync(patientId.Value, dto, ct);

        if (!ok)
            return BadRequest(new { error = err });

        return StatusCode(201, new { success = true, message = "Appointment request submitted." });
    }

    /// <summary>Reschedule an existing appointment.</summary>
    [HttpPut("{id:guid}/reschedule")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] MobileRescheduleBody body, CancellationToken ct)
    {
        if (body is null)
            return BadRequest(new { error = "Request body is required." });

        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        // Verify the appointment belongs to this patient
        var existing = await _portal.GetAppointmentAsync(patientId.Value, id, ct);
        if (existing == null)
            return NotFound();

        if (!DateTime.TryParse(body.NewDate, out var newDate))
            return BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });

        if (!TimeSpan.TryParse(body.NewStartTime, out var newStart) || !TimeSpan.TryParse(body.NewEndTime, out var newEnd))
            return BadRequest(new { error = "Invalid time format. Use HH:mm." });

        // Re-use RequestAppointmentAsync with same doctor but new date/time
        var dto = new PortalAppointmentRequestDto(existing.DoctorId, newDate, newStart, newEnd, existing.Reason);

        // Cancel original then create new
        await _portal.CancelAppointmentAsync(patientId.Value, id, "Reagendada desde app móvil", ct);
        var (ok, err) = await _portal.RequestAppointmentAsync(patientId.Value, dto, ct);

        if (!ok)
            return BadRequest(new { error = err });

        return Ok(new { success = true, message = "Appointment rescheduled." });
    }
}

public record CancelAppointmentRequest(string? Reason);
public record MobileAppointmentRequestBody(Guid DoctorId, string DesiredDate, string StartTime, string EndTime, string? Reason);
public record MobileRescheduleBody(string NewDate, string NewStartTime, string NewEndTime);
