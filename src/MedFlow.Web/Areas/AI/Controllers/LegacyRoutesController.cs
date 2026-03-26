using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.AI.Controllers;

[Area("AI")]
[Authorize]
[Route("AI")]
public class LegacyRoutesController : Controller
{
    [HttpGet("TenantBilling")]
    public IActionResult TenantBilling() => RedirectToAction("Index", "TenantBilling", new { area = "" });

    [HttpGet("Settings")]
    public IActionResult Settings() => RedirectToAction("Index", "Settings", new { area = "" });

    [HttpGet("NotificationTemplates")]
    public IActionResult NotificationTemplates() => RedirectToAction("Index", "NotificationTemplates", new { area = "" });
}
