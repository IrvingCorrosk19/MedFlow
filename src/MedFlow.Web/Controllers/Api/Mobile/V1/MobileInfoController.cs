using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile")]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class MobileInfoController : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType(typeof(MobileApiInfo), 200)]
    public IActionResult GetInfo()
    {
        return Ok(new MobileApiInfo(
            Version: "1.0",
            MinAppVersion: "1.0.0",
            RequiresTenantHeader: true,
            TenantHeaders: ["X-Tenant-ID", "X-Tenant-Code"]));
    }
}

public record MobileApiInfo(string Version, string MinAppVersion, bool RequiresTenantHeader, string[] TenantHeaders);
