using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/push")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class MobilePushController : ControllerBase
{
    private readonly IPushDeviceTokenService _push;
    private readonly IPatientPortalService _portal;
    private readonly ITenantContext _tenant;

    public MobilePushController(IPushDeviceTokenService push, IPatientPortalService portal, ITenantContext tenant)
    {
        _push = push;
        _portal = portal;
        _tenant = tenant;
    }

    [HttpPost("register")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterPushRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Platform))
            return BadRequest(new { error = "Token and Platform are required" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue)
            return BadRequest(new { error = "Tenant not resolved" });

        var patientId = await _portal.GetPatientIdByUserIdAsync(userId, ct);

        await _push.RegisterAsync(new RegisterPushTokenRequest(
            tenantId.Value,
            userId,
            patientId?.ToString(),
            request.Token,
            request.Platform,
            request.DeviceId), ct);

        return NoContent();
    }

    [HttpPost("unregister")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Unregister([FromBody] UnregisterPushRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest();

        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue)
            return BadRequest();

        await _push.UnregisterAsync(tenantId.Value, userId, request.Token, ct);
        return NoContent();
    }
}

public record RegisterPushRequest(string Token, string Platform, string? DeviceId = null);
public record UnregisterPushRequest(string Token);
