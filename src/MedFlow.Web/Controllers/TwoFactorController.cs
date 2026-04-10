using MedFlow.Infrastructure.Identity;
using MedFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Encodings.Web;

namespace MedFlow.Web.Controllers;

[Authorize]
public class TwoFactorController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UrlEncoder _urlEncoder;

    private const string AuthenticatorUriFormat =
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public TwoFactorController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _urlEncoder = urlEncoder;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        ViewBag.Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        ViewBag.HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
        ViewBag.RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        ViewData["Title"] = "Autenticación de dos factores";
        ViewData["PageSubtitle"] = "Seguridad de tu cuenta";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">2FA</li>";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = await BuildEnableAuthenticatorViewModelAsync(user);
        ViewData["Title"] = "Configurar aplicación autenticadora";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildEnableAuthenticatorViewModelAsync(user);
            model.SharedKey = rebuilt.SharedKey;
            model.AuthenticatorUri = rebuilt.AuthenticatorUri;
            return View(model);
        }

        var code = model.Code?.Replace(" ", "").Replace("-", "") ?? string.Empty;
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.Code), "Código de verificación inválido.");
            var rebuilt = await BuildEnableAuthenticatorViewModelAsync(user);
            model.SharedKey = rebuilt.SharedKey;
            model.AuthenticatorUri = rebuilt.AuthenticatorUri;
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _signInManager.RefreshSignInAsync(user);

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["RecoveryCodes"] = recoveryCodes?.ToArray();
        TempData["Success"] = "La autenticación de dos factores está habilitada.";
        return RedirectToAction(nameof(ShowRecoveryCodes));
    }

    [HttpGet]
    public IActionResult ShowRecoveryCodes()
    {
        var codes = TempData["RecoveryCodes"] as string[];
        if (codes == null || codes.Length == 0)
            return RedirectToAction(nameof(Index));

        ViewBag.RecoveryCodes = codes;
        ViewData["Title"] = "Códigos de recuperación";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "La autenticación de dos factores ha sido desactivada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            TempData["Error"] = "No se pueden generar códigos: el 2FA no está habilitado.";
            return RedirectToAction(nameof(Index));
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["RecoveryCodes"] = codes?.ToArray();
        return RedirectToAction(nameof(ShowRecoveryCodes));
    }

    private async Task<EnableAuthenticatorViewModel> BuildEnableAuthenticatorViewModelAsync(ApplicationUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "usuario";
        var sharedKey = FormatKey(unformattedKey!);
        var uri = GenerateQrCodeUri("MedFlow AI", email, unformattedKey!);

        return new EnableAuthenticatorViewModel
        {
            SharedKey = sharedKey,
            AuthenticatorUri = uri
        };
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
            result.Append(unformattedKey.AsSpan(currentPosition));
        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string issuer, string email, string unformattedKey) =>
        string.Format(
            AuthenticatorUriFormat,
            _urlEncoder.Encode(issuer),
            _urlEncoder.Encode(email),
            unformattedKey);
}
