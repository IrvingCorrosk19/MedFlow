using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.Pdf;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;

namespace MedFlow.Web.Controllers;

[Authorize]
public class PrescriptionsController : Controller
{
    private readonly IPrescriptionService _prescriptions;
    private readonly IPatientService _patients;
    private readonly IMedicalRecordService _records;
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenant;

    public PrescriptionsController(
        IPrescriptionService prescriptions,
        IPatientService patients,
        IMedicalRecordService records,
        IClinicSettingsService clinicSettings,
        ITenantContext tenant)
    {
        _prescriptions = prescriptions;
        _patients = patients;
        _records = records;
        _clinicSettings = clinicSettings;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _prescriptions.GetRecentAsync(_tenant.TenantId.Value, 100, ct);
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
            $"<li class=\"breadcrumb-item\"><a href=\"/Prescriptions\">Recetas</a></li>" +
            $"<li class=\"breadcrumb-item active\">Detalle</li>";
        return View(rx);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Create(Guid? medicalRecordId, Guid? patientId, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        var vm = new PrescriptionFormViewModel
        {
            IssuedAt = DateTime.UtcNow.Date,
            PatientId = patientId
        };

        if (medicalRecordId.HasValue)
        {
            vm.MedicalRecordId = medicalRecordId.Value;
            var mr = await _records.GetByIdAsync(medicalRecordId.Value, cancellationToken: ct);
            if (mr != null)
            {
                vm.PatientId = mr.PatientId;
                vm.PrescriberName = mr.Doctor?.FullName;
                vm.PrescriberLicense = mr.Doctor?.LicenseNumber;
            }
        }

        await PopulateDropdowns(vm.PatientId, vm.MedicalRecordId, tid, ct);
        SetViewData("Nueva Receta", "Crear", vm.PatientId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Create(PrescriptionFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model.PatientId, model.MedicalRecordId, tid, ct);
            SetViewData("Nueva Receta", "Crear", model.PatientId);
            return View(model);
        }

        var rx = new Prescription
        {
            TenantId = tid,
            MedicalRecordId = model.MedicalRecordId,
            MedicationName = model.MedicationName,
            Dosage = model.Dosage,
            Frequency = model.Frequency,
            Duration = model.Duration,
            Instructions = model.Instructions,
            PrescriberName = model.PrescriberName,
            PrescriberLicense = model.PrescriberLicense,
            IssuedAt = model.IssuedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(model.IssuedAt, DateTimeKind.Utc)
                : model.IssuedAt.ToUniversalTime()
        };

        await _prescriptions.CreateAsync(rx, ct);
        TempData["Success"] = $"Receta de {rx.MedicationName} creada correctamente.";

        if (model.PatientId.HasValue)
            return RedirectToAction(nameof(ByPatient), new { patientId = model.PatientId.Value });
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        var rx = await _prescriptions.GetByIdAsync(id, tid, ct);
        if (rx == null) return NotFound();
        if (rx.IsVoid)
        {
            TempData["Error"] = "No se puede editar una receta anulada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new PrescriptionFormViewModel
        {
            Id = rx.Id,
            MedicalRecordId = rx.MedicalRecordId,
            PatientId = rx.MedicalRecord?.PatientId,
            MedicationName = rx.MedicationName,
            Dosage = rx.Dosage,
            Frequency = rx.Frequency,
            Duration = rx.Duration,
            Instructions = rx.Instructions,
            PrescriberName = rx.PrescriberName,
            PrescriberLicense = rx.PrescriberLicense,
            IssuedAt = rx.IssuedAt
        };

        await PopulateDropdowns(vm.PatientId, vm.MedicalRecordId, tid, ct);
        SetViewData("Editar Receta", "Editar", vm.PatientId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Edit(PrescriptionFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model.PatientId, model.MedicalRecordId, tid, ct);
            SetViewData("Editar Receta", "Editar", model.PatientId);
            return View(model);
        }

        var rx = await _prescriptions.GetByIdAsync(model.Id!.Value, tid, ct);
        if (rx == null) return NotFound();

        rx.MedicationName = model.MedicationName;
        rx.Dosage = model.Dosage;
        rx.Frequency = model.Frequency;
        rx.Duration = model.Duration;
        rx.Instructions = model.Instructions;
        rx.PrescriberName = model.PrescriberName;
        rx.PrescriberLicense = model.PrescriberLicense;
        rx.IssuedAt = model.IssuedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(model.IssuedAt, DateTimeKind.Utc)
            : model.IssuedAt.ToUniversalTime();

        await _prescriptions.UpdateAsync(rx, ct);
        TempData["Success"] = "Receta actualizada correctamente.";

        return RedirectToAction(nameof(Details), new { id = rx.Id });
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Print(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;

        var rx = await _prescriptions.GetByIdAsync(id, tid, ct);
        if (rx == null) return NotFound();

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

    [HttpGet]
    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> MedicalRecordsByPatient(Guid patientId, CancellationToken ct)
    {
        var records = await _records.GetHistoryByPatientAsync(patientId, ct);
        var result = records.Select(r => new
        {
            id = r.Id,
            display = r.VisitDate.ToString("dd/MM/yyyy") + " — " + (r.ChiefComplaint ?? "Consulta")
        });
        return Json(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task PopulateDropdowns(Guid? patientId, Guid medicalRecordId, Guid tenantId, CancellationToken ct)
    {
        var patients = await _patients.GetAllAsync();
        ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", patientId);

        if (patientId.HasValue)
        {
            var records = await _records.GetHistoryByPatientAsync(patientId.Value, ct);
            ViewBag.MedicalRecords = new SelectList(
                records.Select(r => new { r.Id, Display = r.VisitDate.ToString("dd/MM/yyyy") + " — " + (r.ChiefComplaint ?? "Consulta") }),
                "Id", "Display", medicalRecordId);
        }
        else
        {
            ViewBag.MedicalRecords = new SelectList(Enumerable.Empty<object>(), "Id", "Display");
        }
    }

    private void SetViewData(string title, string action, Guid? patientId)
    {
        ViewData["Title"] = title;
        ViewData["PageSubtitle"] = action == "Crear" ? "Nueva prescripción médica" : "Modificar prescripción";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/Prescriptions\">Recetas</a></li>" +
            $"<li class=\"breadcrumb-item active\">{title}</li>";
    }
}
