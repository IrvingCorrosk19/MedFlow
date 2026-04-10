using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using MedFlow.Web.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace MedFlow.Web.Controllers;

[Authorize]
public class PrescriptionsController : Controller
{
    private readonly IPrescriptionService _prescriptions;
    private readonly IPatientService _patients;
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenant;

    public PrescriptionsController(
        IPrescriptionService prescriptions,
        IPatientService patients,
        IClinicSettingsService clinicSettings,
        ITenantContext tenant)
    {
        _prescriptions = prescriptions;
        _patients = patients;
        _clinicSettings = clinicSettings;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;
        var list = await _prescriptions.GetRecentAsync(tid, 100, ct);
        ViewData["Title"] = "Recetas";
        ViewData["PageSubtitle"] = "Todas las recetas emitidas";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Recetas</li>";
        return View(list);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> ByPatient(Guid patientId, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        var patient = await _patients.GetByIdAsync(patientId);
        if (patient == null) return NotFound();

        var list = await _prescriptions.GetByPatientAsync(patientId, tid, ct);

        ViewBag.Patient = patient;
        ViewData["Title"] = "Recetas";
        ViewData["PageSubtitle"] = patient.NombreCompleto;
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"/Patients\">Pacientes</a></li>" +
            $"<li class=\"breadcrumb-item\"><a href=\"/Patients/Details/{patient.Id}\">{patient.NombreCompleto}</a></li>" +
            $"<li class=\"breadcrumb-item active\">Recetas</li>";
        return View(list);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var rx = await _prescriptions.GetByIdAsync(id, _tenant.TenantId.Value, ct);
        if (rx == null) return NotFound();

        ViewData["Title"] = "Detalle de Receta";
        ViewData["Breadcrumb"] =
            $"<li class=\"breadcrumb-item\"><a href=\"/Patients\">Pacientes</a></li>" +
            $"<li class=\"breadcrumb-item active\">Receta</li>";
        return View(rx);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Print(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        var rx = await _prescriptions.GetByIdAsync(id, tid, ct);
        if (rx == null) return NotFound();

        // Obtener todas las recetas del mismo MedicalRecord para incluirlas en una sola hoja
        var allRx = await _prescriptions.GetByMedicalRecordAsync(rx.MedicalRecordId, ct);

        var settings = await _clinicSettings.GetAsync(tid, ct);
        var patient = rx.MedicalRecord.Patient;

        var doc = new PrescriptionPdfDocument(
            clinicName: settings.Name,
            clinicAddress: settings.Address,
            clinicPhone: settings.Phone,
            patientName: patient?.NombreCompleto ?? "—",
            patientDocument: patient?.NumeroDocumento,
            patientDob: patient?.FechaNacimiento,
            prescriptions: allRx,
            issuedAt: rx.IssuedAt,
            printNumber: rx.PrintCount + 1
        );

        var bytes = doc.GeneratePdf();

        // Incrementar contador de impresiones
        await _prescriptions.IncrementPrintCountAsync(id, ct);

        return File(bytes, "application/pdf",
            $"receta-{patient?.PrimerApellido ?? "paciente"}-{rx.IssuedAt:yyyyMMdd}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Void(Guid id, string reason, Guid patientId, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        await _prescriptions.VoidAsync(id, _tenant.TenantId.Value, reason ?? "Anulada por el médico", ct);
        TempData["Success"] = "Receta anulada correctamente.";
        return RedirectToAction(nameof(ByPatient), new { patientId });
    }
}
