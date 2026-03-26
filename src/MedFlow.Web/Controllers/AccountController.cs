using MedFlow.Infrastructure.Identity;
using MedFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new AccountLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AccountLoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Login: ModelState inválido para {Email}", model.Email);
            return View(model);
        }

        _logger.LogInformation("Login intento: {Email}", model.Email);
        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Count == 1 && roles.Any(r => string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)))
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty,
                        "Use el portal del paciente (/PatientPortal/Auth/Login) para iniciar sesión con esta cuenta.");
                    return View(model);
                }
            }

            if (Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        _logger.LogWarning("Login fallido para {Email}: Succeeded={Succeeded}, IsLockedOut={Locked}, IsNotAllowed={NotAllowed}",
            model.Email, result.Succeeded, result.IsLockedOut, result.IsNotAllowed);

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada por intentos fallidos. Intente más tarde.");
        else if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, "No se permite el inicio de sesión con esta cuenta.");
        else
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }
}
