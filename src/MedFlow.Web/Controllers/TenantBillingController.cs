using MedFlow.Application.Billing;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
public class TenantBillingController : Controller
{
    private readonly ITenantContext _tenant;
    private readonly ISaasBillingQueryService _queries;
    private readonly ISaasBillingService _billing;
    private readonly ISubscriptionPlanAdminService _plans;

    public TenantBillingController(
        ITenantContext tenant,
        ISaasBillingQueryService queries,
        ISaasBillingService billing,
        ISubscriptionPlanAdminService plans)
    {
        _tenant = tenant;
        _queries = queries;
        _billing = billing;
        _plans = plans;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        var model = await _queries.GetTenantBillingPortalAsync(tenantId.Value, cancellationToken);
        if (model == null) return NotFound();

        ViewData["Title"] = "Mi suscripción";
        ViewData["PageSubtitle"] = "Plan, facturación y uso";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Suscripción</li>";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckout(Guid planId, BillingPeriod period, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = Url.Action(nameof(Index), "TenantBilling", null, Request.Scheme) ?? baseUrl;
        var cancelUrl = Url.Action(nameof(Index), "TenantBilling", null, Request.Scheme) ?? baseUrl;

        try
        {
            var result = await _billing.CreateCheckoutSessionAsync(tenantId.Value, planId, period, successUrl, cancelUrl, cancellationToken);
            return Redirect(result.SessionUrl);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToUserFriendlyBillingError(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(bool atPeriodEnd, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        try
        {
            await _billing.CancelSubscriptionAsync(tenantId.Value, atPeriodEnd, cancellationToken);
            TempData["Success"] = atPeriodEnd ? "Su suscripción se cancelará al final del período actual." : "Suscripción cancelada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToUserFriendlyBillingError(ex);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        try
        {
            await _billing.ResumeSubscriptionAsync(tenantId.Value, cancellationToken);
            TempData["Success"] = "Suscripción reactivada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToUserFriendlyBillingError(ex);
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> UpdateProfile(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        TenantBillingProfileDto? profile;
        try
        {
            profile = await _billing.GetOrCreateBillingProfileAsync(tenantId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ToUserFriendlyBillingError(ex);
            return RedirectToAction(nameof(Index));
        }
        if (profile == null) return NotFound();

        ViewData["Title"] = "Datos de facturación";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Suscripción</a></li><li class=\"breadcrumb-item active\">Datos de facturación</li>";

        return View(new TenantBillingProfileEditViewModel
        {
            BillingEmail = profile.BillingEmail,
            LegalName = profile.LegalName,
            TaxId = profile.TaxId,
            Country = profile.Country,
            AddressLine1 = profile.AddressLine1,
            PreferredCurrency = profile.PreferredCurrency ?? "USD"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(TenantBillingProfileEditViewModel model, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        try
        {
            await _billing.UpdateBillingProfileAsync(tenantId.Value, new TenantBillingProfileUpdateDto(
                model.BillingEmail, model.LegalName, model.TaxId, model.Country,
                model.StateProvince, model.City, model.AddressLine1, model.AddressLine2,
                model.PostalCode, model.PreferredCurrency), cancellationToken);
            TempData["Success"] = "Datos de facturación actualizados.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ToUserFriendlyBillingError(ex));
            return View(model);
        }
    }

    private static string ToUserFriendlyBillingError(Exception ex)
    {
        var msg = ex.Message ?? "";
        if (msg.Contains("No API key provided", StringComparison.OrdinalIgnoreCase))
        {
            return "Stripe no está configurado. Defina Stripe:SecretKey en la configuración (appsettings/user-secrets/variables de entorno) y reinicie la aplicación.";
        }
        return msg;
    }
}
