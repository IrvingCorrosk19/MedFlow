using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class BillingController : Controller
{
    private readonly IPatientPortalService _portal;

    public BillingController(IPatientPortalService portal) => _portal = portal;

    [HttpGet]
    [Route("facturas")]
    public async Task<IActionResult> Invoices(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");
        if (!options.ShowBilling) { TempData["Warning"] = "El módulo de facturación no está disponible."; return RedirectToAction("Index", "Home"); }

        var invoices = await _portal.GetInvoicesAsync(patientId.Value, ct);
        var (balanceDue, _) = await _portal.GetAccountStatusAsync(patientId.Value, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["BalanceDue"] = balanceDue;
        return View(invoices);
    }

    [HttpGet]
    [Route("facturas/{id:guid}")]
    public async Task<IActionResult> InvoiceDetail(Guid id, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var invoice = await _portal.GetInvoiceAsync(patientId.Value, id, ct);
        if (invoice == null) { TempData["Error"] = "Factura no encontrada."; return RedirectToAction("Invoices"); }

        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;
        return View(invoice);
    }

    [HttpGet]
    [Route("pagos")]
    public async Task<IActionResult> Payments(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");
        if (!options.ShowBilling) { TempData["Warning"] = "El módulo de facturación no está disponible."; return RedirectToAction("Index", "Home"); }

        var payments = await _portal.GetPaymentsAsync(patientId.Value, 50, ct);
        var (balanceDue, totalPaid) = await _portal.GetAccountStatusAsync(patientId.Value, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["BalanceDue"] = balanceDue;
        ViewData["TotalPaid"] = totalPaid;
        return View(payments);
    }

    [HttpGet]
    [Route("estado-cuenta")]
    public async Task<IActionResult> AccountStatus(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.ShowBilling) { TempData["Warning"] = "El módulo de facturación no está disponible."; return RedirectToAction("Index", "Home"); }

        var (balanceDue, totalPaid) = await _portal.GetAccountStatusAsync(patientId.Value, ct);
        var invoices = await _portal.GetInvoicesAsync(patientId.Value, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["BalanceDue"] = balanceDue;
        ViewData["TotalPaid"] = totalPaid;
        ViewData["Invoices"] = invoices;
        ViewData["Title"] = "Estado de cuenta";
        ViewData["PageSubtitle"] = "Resumen financiero";
        return View("AccountStatus");
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
