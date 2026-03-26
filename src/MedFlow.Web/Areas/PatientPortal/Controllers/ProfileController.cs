using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using MedFlow.Web.Areas.PatientPortal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class ProfileController : Controller
{
    private readonly IPatientPortalService _portal;

    public ProfileController(IPatientPortalService portal) => _portal = portal;

    [HttpGet]
    [Route("perfil")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");

        var profile = await _portal.GetProfileAsync(patientId.Value, ct);
        if (profile == null)
        {
            TempData["Error"] = "Perfil no encontrado.";
            return RedirectToAction("Index", "Home");
        }

        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;
        ViewData["AllowEdit"] = options.AllowProfileEdit;
        return View(profile);
    }

    [HttpPost]
    [Route("perfil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PatientProfileUpdateViewModel model, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.AllowProfileEdit)
        {
            TempData["Error"] = "La edición del perfil no está permitida.";
            return RedirectToAction("Index");
        }

        var profile = await _portal.GetProfileAsync(patientId.Value, ct);
        if (profile == null) return RedirectToAction("Index", "Home");

        if (ModelState.IsValid)
        {
            await _portal.UpdateProfileAsync(patientId.Value, new PatientProfileUpdateDto(
                model.Telefono, model.Correo, model.Direccion,
                model.ContactoEmergenciaNombre, model.ContactoEmergenciaTelefono), ct);
            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Index");
        }

        ViewData["AllowEdit"] = true;
        return View(profile);
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
