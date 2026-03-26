using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePlanFeature(PlanFeatureKind.Billing)]
public class PaymentsController : Controller
{
    private readonly IPaymentService _payments;
    private readonly IBillingInvoiceService _billing;
    private readonly IPatientService _patients;

    public PaymentsController(IPaymentService payments, IBillingInvoiceService billing, IPatientService patients)
    {
        _payments = payments;
        _billing = billing;
        _patients = patients;
    }

    public async Task<IActionResult> Index(Guid? invoiceId, Guid? patientId, DateTime? from, DateTime? to, ClinicalPaymentMethod? method, CancellationToken cancellationToken = default)
    {
        var list = await _payments.SearchAsync(invoiceId, patientId, from, to, method, cancellationToken);
        var patients = await _patients.GetAllAsync();
        ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", patientId);
        ViewBag.InvoiceId = invoiceId;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Method = method;
        ViewData["Title"] = "Pagos";
        ViewData["PageSubtitle"] = "Historial de cobros";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Pagos</li>";
        return View(list);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.BillingRegisterPayment)]
    public async Task<IActionResult> Create(Guid? billingInvoiceId, CancellationToken cancellationToken = default)
    {
        if (billingInvoiceId.HasValue)
        {
            var inv = await _billing.GetByIdAsync(billingInvoiceId.Value, cancellationToken);
            if (inv != null)
            {
                var vm = new RegisterPaymentViewModel
                {
                    BillingInvoiceId = inv.Id,
                    PatientId = inv.PatientId,
                    Amount = inv.BalanceDue > 0 ? inv.BalanceDue : 0,
                    PaymentDate = DateTime.UtcNow
                };
                ViewData["Title"] = "Registrar pago";
                ViewData["PageSubtitle"] = $"Factura {inv.InvoiceNumber}";
                ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Payments") + "\">Pagos</a></li><li class=\"breadcrumb-item active\">Registrar</li>";
                return View(vm);
            }
        }

        ViewData["Title"] = "Registrar pago";
        ViewData["PageSubtitle"] = "Seleccione una factura desde el detalle";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Pagos</li>";
        return View(new RegisterPaymentViewModel { PaymentDate = DateTime.UtcNow });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterPaymentViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Registrar pago";
            return View(model);
        }

        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (p, err) = await _payments.RegisterAsync(
            model.BillingInvoiceId,
            model.PatientId,
            model.PaymentDate,
            model.Amount,
            model.PaymentMethod,
            model.ReferenceNumber,
            model.Notes,
            uid,
            cancellationToken);

        if (err != null)
        {
            ModelState.AddModelError(string.Empty, err);
            ViewData["Title"] = "Registrar pago";
            return View(model);
        }

        TempData["Success"] = "Pago registrado.";
        return RedirectToAction(nameof(Details), new { id = p!.Id });
    }

    [RequirePermission(PermissionCodes.BillingView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _payments.GetByIdAsync(id, cancellationToken);
        if (p == null)
            return NotFound();

        ViewData["Title"] = "Pago";
        ViewData["PageSubtitle"] = p.ReferenceNumber ?? p.Id.ToString();
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index") + "\">Pagos</a></li><li class=\"breadcrumb-item active\">Detalle</li>";
        return View(p);
    }
}
