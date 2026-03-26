using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api;

[ApiController]
[Route("api/v1/auth/staff")]
public sealed class TenantStaffAuthController : ControllerBase
{
    private readonly ITenantStaffAuthService _auth;

    public TenantStaffAuthController(ITenantStaffAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>JWT + refresh para personal de clínica (no Patient). Requiere tenantCode.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StaffJwtLoginResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] StaffJwtLoginRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password) || string.IsNullOrWhiteSpace(body.TenantCode))
            return BadRequest(new { error = "email, password y tenantCode son obligatorios." });

        var result = await _auth.LoginAsync(body, ct);
        if (result == null)
            return Unauthorized(new { error = "Credenciales inválidas, tenant incorrecto o rol no permitido (use login paciente para Patient)." });

        return Ok(result);
    }
}
