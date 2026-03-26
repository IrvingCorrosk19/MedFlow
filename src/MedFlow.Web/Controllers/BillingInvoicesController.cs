using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePlanFeature(PlanFeatureKind.Billing)]
public class BillingInvoicesController : Controller
{
    private readonly IBillingInvoiceService _billing;
    private readonly IPaymentService _payments;
    private readonly IPatientService _patients;
    private readonly IAppointmentService _appointments;
    private readonly IDoctorService _doctors;

    public BillingInvoicesController(
        IBillingInvoiceService billing,
        IPaymentService payments,
        IPatientService patients,
        IAppointmentService appointments,
        IDoctorService doctors)
    {
        _billing = billing;
        _payments = payments;
        _patients = patients;
        _appointments = appointments;
        _doctors = doctors;
    }

    [RequirePermission(PermissionCodes.BillingView)]
    public async Task<IActionResult> Index(Guid? patientId, DateTime? from, DateTime? to, InvoiceStatus? status, CancellationToken cancellationToken = default)
    {
        var list = await _billing.SearchAsync(patientId, from, to, status, cancellationToken);
        var patients = await _patients.GetAllAsync();
        ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", patientId);
        ViewBag.PatientId = patientId;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewData["Title"] = "Facturas";
        ViewData["PageSubtitle"] = "Cobros clínicos y consultas";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Facturas</li>";
        return View(list);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.BillingCreate)]
    public async Task<IActionResult> Create(Guid? patientId, Guid? appointmentId, CancellationToken cancellationToken)
    {
        var patients = await _patients.GetAllAsync();
        var doctors = await _doctors.GetAllAsync();

        ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", patientId);
        ViewBag.Doctors = new SelectList(doctors, "Id", "FullName");

        var vm = new BillingInvoiceCreateViewModel
        {
            PatientId = patientId ?? Guid.Empty,
            IssueDate = DateTime.UtcNow.Date,
            Lines = new List<BillingLineInputViewModel>
            {
                new()
                {
                    ItemType = BillingInvoiceItemType.ConsultationGeneral,
                    Description = "Consulta médica",
                    Quantity = 1,
                    UnitPrice = 0
                }
            }
        };

        if (appointmentId.HasValue)
        {
            var apt = await _appointments.GetByIdAsync(appointmentId.Value, cancellationToken);
            if (apt != null)
            {
                vm.PatientId = apt.PatientId;
                vm.AppointmentId = apt.Id;
                vm.DoctorId = apt.DoctorId;
            }
        }

        ViewData["Title"] = "Nueva factura";
        ViewData["PageSubtitle"] = "Registro de cobro";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index") + "\">Facturas</a></li><li class=\"breadcrumb-item active\">Nueva</li>";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillingInvoiceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (model.Lines == null || model.Lines.Count == 0)
            ModelState.AddModelError("Lines", "Debe agregar al menos una línea de factura.");

        if (!ModelState.IsValid)
        {
            var patients = await _patients.GetAllAsync();
            var doctors = await _doctors.GetAllAsync();
            ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", model.PatientId);
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", model.DoctorId);
            ViewData["Title"] = "Nueva factura";
            return View(model);
        }

        var invoice = new BillingInvoice
        {
            PatientId = model.PatientId,
            AppointmentId = model.AppointmentId,
            DoctorId = model.DoctorId,
            IssueDate = model.IssueDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(model.IssueDate, DateTimeKind.Utc)
                : model.IssueDate.ToUniversalTime(),
            DueDate = model.DueDate.HasValue
                ? (model.DueDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Utc)
                    : model.DueDate.Value.ToUniversalTime())
                : null,
            DiscountAmount = model.DiscountAmount,
            TaxAmount = model.TaxAmount,
            Notes = model.Notes
        };

        var items = model.Lines!.Select(l => new BillingInvoiceItem
        {
            ItemType = l.ItemType,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            DiscountAmount = l.LineDiscount
        }).ToList();

        var (created, err) = await _billing.CreateAsync(invoice, items, cancellationToken);
        if (err != null)
        {
            ModelState.AddModelError(string.Empty, err);
            var patients = await _patients.GetAllAsync();
            var doctors = await _doctors.GetAllAsync();
            ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", model.PatientId);
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", model.DoctorId);
            ViewData["Title"] = "Nueva factura";
            return View(model);
        }

        TempData["Success"] = $"Factura {created.InvoiceNumber} creada.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [RequirePermission(PermissionCodes.BillingView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var inv = await _billing.GetByIdAsync(id, cancellationToken);
        if (inv == null)
            return NotFound();

        var vm = new RegisterPaymentViewModel
        {
            BillingInvoiceId = inv.Id,
            PatientId = inv.PatientId,
            Amount = inv.BalanceDue > 0 ? inv.BalanceDue : 0,
            PaymentDate = DateTime.UtcNow
        };

        ViewBag.RegisterPayment = vm;
        ViewData["Title"] = inv.InvoiceNumber;
        ViewData["PageSubtitle"] = "Detalle de factura";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index") + "\">Facturas</a></li><li class=\"breadcrumb-item active\">" + inv.InvoiceNumber + "</li>";
        return View(inv);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.BillingRegisterPayment)]
    public async Task<IActionResult> RegisterPayment(RegisterPaymentViewModel model, CancellationToken cancellationToken)
    {
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
            TempData["Error"] = err;
        else
            TempData["Success"] = "Pago registrado correctamente.";

        return RedirectToAction(nameof(Details), new { id = model.BillingInvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayment(Guid id, Guid billingInvoiceId, CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (ok, err) = await _payments.CancelPaymentAsync(id, uid, cancellationToken);
        if (!ok)
            TempData["Error"] = err;
        else
            TempData["Success"] = "Pago anulado.";
        return RedirectToAction(nameof(Details), new { id = billingInvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.BillingCancel)]
    public async Task<IActionResult> CancelInvoice(Guid id, CancellationToken cancellationToken)
    {
        var (ok, err) = await _billing.CancelAsync(id, cancellationToken);
        if (!ok)
            TempData["Error"] = err;
        else
            TempData["Success"] = "Factura cancelada.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
