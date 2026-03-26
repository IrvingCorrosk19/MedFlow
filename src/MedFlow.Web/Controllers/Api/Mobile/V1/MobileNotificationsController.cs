using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/notifications")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class MobileNotificationsController : ControllerBase
{
    private readonly IPatientPortalService _portal;

    public MobileNotificationsController(IPatientPortalService portal)
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PatientNotificationItemDto>), 200)]
    public async Task<IActionResult> GetList([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var list = await _portal.GetNotificationsAsync(patientId.Value, skip, take, ct);
        return Ok(list);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), 200)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var count = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        return Ok(new UnreadCountResponse(count));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        await _portal.MarkNotificationReadAsync(patientId.Value, id, ct);
        return NoContent();
    }
}

public record UnreadCountResponse(int Count);
