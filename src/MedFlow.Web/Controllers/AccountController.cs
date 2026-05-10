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
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var staffRoles = roles.Where(static r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)).ToList();
                if (roles.Count > 0 && staffRoles.Count == 0)
                    return RedirectToAction("Index", "Home", new { area = "PatientPortal" });
            }

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
                // Solo rol Patient (sin Admin/Doctor/etc.): no usar login de personal — evita redirect al Dashboard y bucle 403.
                var staffRoles = roles.Where(static r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)).ToList();
                var hasPatient = roles.Any(static r => string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase));
                if (roles.Count > 0 && hasPatient && staffRoles.Count == 0)
                {
                    await _signInManager.SignOutAsync();
                    TempData["PortalLoginHint"] =
                        "Las cuentas de paciente deben entrar por el Portal del paciente. Use la misma contraseña en la siguiente pantalla.";
                    return RedirectToAction("Login", "Auth", new { area = "PatientPortal" });
                }
            }

            if (Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            // Don't reveal whether the user exists
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action(nameof(ResetPassword), "Account",
            new { token, email = model.Email }, Request.Scheme);

        _logger.LogInformation("Password reset requested for {Email}. Token URL: {Url}", model.Email, callbackUrl);
        // In production: send email with callbackUrl
        // For now, show the link in TempData for development
        TempData["ResetLink"] = callbackUrl;

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token, string? email)
    {
        if (token == null || email == null)
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login));

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.RememberMe = rememberMe;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.RememberMe = rememberMe;
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login));

        var code = model.TwoFactorCode?.Replace(" ", "").Replace("-", "") ?? string.Empty;
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, rememberMe, model.RememberMachine);

        if (result.Succeeded)
        {
            if (Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada por intentos fallidos.");
        else
            ModelState.AddModelError(string.Empty, "Código de verificación inválido.");

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.RememberMe = rememberMe;
        return View(model);
    }
}
