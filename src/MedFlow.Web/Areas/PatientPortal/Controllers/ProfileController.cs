using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using MedFlow.Infrastructure.Identity;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using MedFlow.Web.Areas.PatientPortal.Models;
using MedFlow.Web.Areas.PatientPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class ProfileController : Controller
{
    private readonly IPatientPortalService _portal;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(IPatientPortalService portal, UserManager<ApplicationUser> userManager)
    {
        _portal = portal;
        _userManager = userManager;
    }

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

    [HttpGet]
    [Route("perfil/cambiar-contrasena")]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Cambiar contraseña";
        ViewData["PageSubtitle"] = "Actualiza tu contraseña de acceso";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/PatientPortal/perfil\">Mi perfil</a></li><li class=\"breadcrumb-item active\">Cambiar contraseña</li>";
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Route("perfil/cambiar-contrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Cambiar contraseña";
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            ViewData["Title"] = "Cambiar contraseña";
            return View(model);
        }

        TempData["Success"] = "Contraseña actualizada correctamente.";
        return RedirectToAction("Index");
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
