using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/patient")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class MobilePatientController : ControllerBase
{
    private readonly IPatientPortalService _portal;
    private readonly ITenantContext _tenant;

    public MobilePatientController(IPatientPortalService portal, ITenantContext tenant)
    {
        _portal = portal;
        _tenant = tenant;
    }

    private async Task<Guid?> GetPatientIdAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _portal.GetPatientIdByUserIdAsync(userId, ct);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(PatientPortalDashboardDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var dash = await _portal.GetDashboardAsync(patientId.Value, ct);
        if (dash == null)
            return NotFound();

        return Ok(dash);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(PatientProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var profile = await _portal.GetProfileAsync(patientId.Value, ct);
        if (profile == null)
            return NotFound();

        return Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateProfile([FromBody] PatientProfileUpdateDto dto, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        await _portal.UpdateProfileAsync(patientId.Value, dto, ct);
        return NoContent();
    }
}
