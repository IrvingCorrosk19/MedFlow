using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MedFlow.Infrastructure.Identity;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITenantContext _tenant;
    private readonly IPatientPortalService _portal;

    public AuthController(SignInManager<ApplicationUser> signInManager, ITenantContext tenant, IPatientPortalService portal)
    {
        _signInManager = signInManager;
        _tenant = tenant;
        _portal = portal;
    }

    [HttpGet]
    [Route("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Patient"))
            return RedirectToAction("Index", "Home", new { area = "PatientPortal" });

        return View(new PatientLoginViewModel { ReturnUrl = returnUrl ?? Url.Action("Index", "Home", new { area = "PatientPortal" }) });
    }

    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PatientLoginViewModel model, string? returnUrl, CancellationToken ct)
    {
        model.ReturnUrl ??= returnUrl ?? Url.Action("Index", "Home", new { area = "PatientPortal" });

        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
            return View(model);
        }

        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
        if (user == null || !await _signInManager.UserManager.IsInRoleAsync(user, "Patient"))
        {
            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Esta área es solo para pacientes. Use el acceso de personal si es empleado.");
            return View(model);
        }

        var patientId = await _portal.GetPatientIdByUserIdAsync(user.Id, ct);
        if (!patientId.HasValue)
        {
            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "No hay perfil de paciente asociado a esta cuenta.");
            return View(model);
        }

        var options = await _portal.GetOptionsAsync(user.TenantId ?? Guid.Empty, ct);
        if (!options.Enabled)
        {
            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "El portal del paciente no está disponible en este momento.");
            return View(model);
        }

        if (user.TenantId.HasValue && _tenant.TenantId != user.TenantId)
        {
            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "El tenant de la sesión no coincide. Acceda desde el dominio correcto de su clínica.");
            return View(model);
        }

        return LocalRedirect(model.ReturnUrl!);
    }

    [HttpPost]
    [Route("logout")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", new { returnUrl });
    }

    [HttpGet]
    [Route("acceso-denegado")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
