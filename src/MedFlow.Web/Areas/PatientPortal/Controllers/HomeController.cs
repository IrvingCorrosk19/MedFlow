using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class HomeController : Controller
{
    private readonly IPatientPortalService _portal;

    public HomeController(IPatientPortalService portal) => _portal = portal;

    [HttpGet]
    [Route("")]
    [Route("inicio")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled)
        {
            TempData["Error"] = "El portal del paciente no está disponible.";
            return RedirectToAction("Login", "Auth");
        }

        var dashboard = await _portal.GetDashboardAsync(patientId.Value, ct);
        if (dashboard == null)
        {
            TempData["Error"] = "No se pudo cargar la información.";
            return RedirectToAction("Login", "Auth");
        }

        HttpContext.Items["UnreadNotificationsCount"] = dashboard.UnreadNotificationsCount;
        ViewData["Options"] = options;
        return View(dashboard);
    }

    private Guid? GetPatientId()
    {
        var claim = User.FindFirst("patient_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
