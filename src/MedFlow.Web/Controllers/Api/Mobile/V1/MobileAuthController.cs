using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly IMobileAuthService _auth;

    public MobileAuthController(IMobileAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MobileLoginResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required" });

        var result = await _auth.LoginAsync(request, ct);
        if (result == null)
            return Unauthorized(new { error = "Invalid credentials or portal disabled" });

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MobileLoginResult), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
            return BadRequest(new { error = "RefreshToken is required" });

        var result = await _auth.RefreshAsync(body.RefreshToken, ct);
        if (result == null)
            return Unauthorized(new { error = "Invalid or expired refresh token" });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? body, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _auth.LogoutAsync(userId, body?.RefreshToken, ct);
        return NoContent();
    }
}

public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string? RefreshToken);
