using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.Pdf;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace MedFlow.Web.Controllers;

[Authorize]
public class MedicalRecordsController : Controller
{
    private readonly IMedicalRecordService _medicalRecords;
    private readonly IPatientService _patients;
    private readonly IDoctorService _doctors;
    private readonly IWebHostEnvironment _env;
    private readonly IApplicationDbContext _db;
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenantContext;

    public MedicalRecordsController(
        IMedicalRecordService medicalRecords,
        IPatientService patients,
        IDoctorService doctors,
        IWebHostEnvironment env,
        IApplicationDbContext db,
        IClinicSettingsService clinicSettings,
        ITenantContext tenantContext)
    {
        _medicalRecords = medicalRecords;
        _patients = patients;
        _doctors = doctors;
        _env = env;
        _db = db;
        _clinicSettings = clinicSettings;
        _tenantContext = tenantContext;
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Historia clínica";
        ViewData["PageSubtitle"] = "Seleccione un paciente para ver su expediente";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Historia clínica</li>";

        var patients = await _patients.GetAllAsync(search, true, cancellationToken: cancellationToken);
        ViewBag.Search = search;
        return View(patients);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Patient(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(patientId, cancellationToken);
        if (patient == null) return NotFound();

        var history = await _medicalRecords.GetHistoryByPatientAsync(patientId, cancellationToken);

        ViewData["Title"] = $"Expediente · {patient.NombreCompleto}";
        ViewData["PageSubtitle"] = "Historial cronológico de consultas";
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"{Url.Action(nameof(Index))}\">Historia clínica</a></li><li class=\"breadcrumb-item active\">{patient.NombreCompleto}</li>";

        ViewBag.Patient = patient;
        return View(history);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var record = await _medicalRecords.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (record == null) return NotFound();

        ViewData["Title"] = $"Consulta {record.VisitDate:dd/MM/yyyy HH:mm}";
        ViewData["PageSubtitle"] = record.Patient?.NombreCompleto;
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"{Url.Action(nameof(Patient), new { patientId = record.PatientId })}\">Expediente</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        return View(record);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsCreate)]
    public async Task<IActionResult> Create(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(patientId, cancellationToken);
        if (patient == null) return NotFound();

        var doctors = await _doctors.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Doctors = doctors;
        ViewBag.Patient = patient;
        ViewBag.Appointments = await _db.Appointments
            .Where(a => a.PatientId == patientId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .Take(30)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Nueva consulta";
        ViewData["PageSubtitle"] = patient.NombreCompleto;
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"{Url.Action(nameof(Patient), new { patientId })}\">Expediente</a></li><li class=\"breadcrumb-item active\">Nueva consulta</li>";

        return View(new MedicalRecordFormViewModel
        {
            PatientId = patientId,
            VisitDate = DateTime.Now,
            PrescriptionLines = [new PrescriptionLineViewModel(), new PrescriptionLineViewModel()]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsCreate)]
    public async Task<IActionResult> Create(MedicalRecordFormViewModel model, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(model.PatientId, cancellationToken);
        if (patient == null) return NotFound();

        if (model.PrescriptionLines != null)
        {
            for (int i = 0; i < model.PrescriptionLines.Count; i++)
            {
                var line = model.PrescriptionLines[i];
                bool hasOtherFields = !string.IsNullOrWhiteSpace(line.Dosage)
                    || !string.IsNullOrWhiteSpace(line.Frequency)
                    || !string.IsNullOrWhiteSpace(line.Duration)
                    || !string.IsNullOrWhiteSpace(line.Instructions);
                if (hasOtherFields && string.IsNullOrWhiteSpace(line.MedicationName))
                    ModelState.AddModelError($"PrescriptionLines[{i}].MedicationName",
                        "El nombre del medicamento es obligatorio.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var entity = MapToEntity(model);
                var rx = MapPrescriptions(model);
                await _medicalRecords.CreateAsync(entity, rx, cancellationToken);
                TempData["Success"] = "Consulta registrada correctamente.";
                return RedirectToAction(nameof(Patient), new { patientId = model.PatientId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al registrar la consulta: " + ex.Message);
            }
        }

        var doctors = await _doctors.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Doctors = doctors;
        ViewBag.Patient = patient;
        ViewBag.Appointments = await _db.Appointments
            .Where(a => a.PatientId == model.PatientId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .Take(30)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Nueva consulta";
        ViewData["PageSubtitle"] = patient.NombreCompleto;
        return View(model);
    }

    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var record = await _medicalRecords.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (record == null) return NotFound();

        var patient = await _patients.GetByIdAsync(record.PatientId, cancellationToken);
        if (patient == null) return NotFound();

        var doctors = await _doctors.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Doctors = doctors;
        ViewBag.Patient = patient;
        ViewBag.Appointments = await _db.Appointments
            .Where(a => a.PatientId == record.PatientId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .Take(30)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Editar consulta";
        ViewData["PageSubtitle"] = patient.NombreCompleto;
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a href=\"{Url.Action(nameof(Details), new { id })}\">Consulta</a></li><li class=\"breadcrumb-item active\">Editar</li>";

        return View(MapToForm(record));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> Edit(Guid id, MedicalRecordFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return NotFound();

        var existing = await _medicalRecords.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (existing == null) return NotFound();

        if (model.PrescriptionLines != null)
        {
            for (int i = 0; i < model.PrescriptionLines.Count; i++)
            {
                var line = model.PrescriptionLines[i];
                bool hasOtherFields = !string.IsNullOrWhiteSpace(line.Dosage)
                    || !string.IsNullOrWhiteSpace(line.Frequency)
                    || !string.IsNullOrWhiteSpace(line.Duration)
                    || !string.IsNullOrWhiteSpace(line.Instructions);
                if (hasOtherFields && string.IsNullOrWhiteSpace(line.MedicationName))
                    ModelState.AddModelError($"PrescriptionLines[{i}].MedicationName",
                        "El nombre del medicamento es obligatorio.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                MapToEntity(model, existing);
                var rx = MapPrescriptions(model);
                await _medicalRecords.UpdateAsync(existing, rx, cancellationToken);
                TempData["Success"] = "Consulta actualizada correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al actualizar la consulta: " + ex.Message);
            }
        }

        var patient = await _patients.GetByIdAsync(model.PatientId, cancellationToken);
        ViewBag.Doctors = await _doctors.GetAllAsync(null, true, cancellationToken: cancellationToken);
        ViewBag.Patient = patient;
        ViewBag.Appointments = await _db.Appointments
            .Where(a => a.PatientId == model.PatientId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .Take(30)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Editar consulta";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.MedicalRecordsDelete)]
    public async Task<IActionResult> Delete(Guid id, Guid patientId, CancellationToken cancellationToken)
    {
        await _medicalRecords.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Registro archivado.";
        return RedirectToAction(nameof(Patient), new { patientId });
    }

    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        var record = await _medicalRecords.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (record == null) return NotFound();

        var settings = _tenantContext.TenantId.HasValue
            ? await _clinicSettings.GetAsync(_tenantContext.TenantId.Value, cancellationToken)
            : null;

        var doc = new MedicalRecordPdfDocument(
            settings?.Name ?? "MedFlow",
            settings?.Address,
            settings?.Phone,
            record);

        var bytes = doc.GeneratePdf();
        var fileName = $"HistoriaClinica_{record.Patient?.NombreCompleto?.Replace(" ", "_") ?? record.PatientId.ToString("N")}_{record.VisitDate:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    [RequirePermission(PermissionCodes.MedicalRecordsEdit)]
    public async Task<IActionResult> UploadAttachment(Guid medicalRecordId, IFormFile file, CancellationToken cancellationToken)
    {
        var record = await _medicalRecords.GetByIdAsync(medicalRecordId, cancellationToken: cancellationToken);
        if (record == null) return NotFound();
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Seleccione un archivo.";
            return RedirectToAction(nameof(Details), new { id = medicalRecordId });
        }

        var safeName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(safeName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var relative = Path.Combine("uploads", "medical", medicalRecordId.ToString("N"));
        var physical = Path.Combine(_env.WebRootPath, relative);
        Directory.CreateDirectory(physical);
        var fullPath = Path.Combine(physical, stored);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var att = new MedicalAttachment
        {
            MedicalRecordId = medicalRecordId,
            FileName = safeName,
            FilePath = "/" + relative.Replace("\\", "/") + "/" + stored,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow
        };
        await _db.MedicalAttachments.AddAsync(att, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Archivo adjuntado.";
        return RedirectToAction(nameof(Details), new { id = medicalRecordId });
    }

    private static MedicalRecord MapToEntity(MedicalRecordFormViewModel vm, MedicalRecord? entity = null)
    {
        entity ??= new MedicalRecord();
        entity.PatientId = vm.PatientId;
        entity.DoctorId = vm.DoctorId;
        entity.AppointmentId = vm.AppointmentId;
        entity.VisitDate = vm.VisitDate;
        entity.ChiefComplaint = vm.ChiefComplaint;
        entity.Diagnosis = vm.Diagnosis;
        entity.TreatmentPlan = vm.TreatmentPlan;
        entity.ClinicalNotes = vm.ClinicalNotes;
        entity.Observations = vm.Observations;
        entity.HeightCm = vm.HeightCm;
        entity.WeightKg = vm.WeightKg;
        entity.BloodPressure = vm.BloodPressure;
        entity.HeartRateBpm = vm.HeartRateBpm;
        entity.TemperatureCelsius = vm.TemperatureCelsius;
        return entity;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.MedicalRecordsView)]
    public async Task<IActionResult> Search(string? q, Guid? patientId, Guid? doctorId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Búsqueda en historia clínica";
        ViewData["PageSubtitle"] = "Busca en notas, diagnósticos y planes de tratamiento";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Historia clínica</a></li><li class=\"breadcrumb-item active\">Búsqueda</li>";

        if (string.IsNullOrWhiteSpace(q) && !patientId.HasValue && !doctorId.HasValue)
        {
            ViewBag.Q = q;
            return View(Array.Empty<MedicalRecord>());
        }

        var results = await _medicalRecords.SearchAsync(q ?? "", patientId, doctorId, cancellationToken);

        ViewBag.Q = q;
        ViewBag.PatientId = patientId;
        ViewBag.DoctorId = doctorId;
        return View(results);
    }

    private static List<Prescription> MapPrescriptions(MedicalRecordFormViewModel vm)
    {
        var list = new List<Prescription>();
        foreach (var line in vm.PrescriptionLines)
        {
            if (string.IsNullOrWhiteSpace(line.MedicationName)) continue;
            list.Add(new Prescription
            {
                MedicationName = line.MedicationName.Trim(),
                Dosage = line.Dosage,
                Frequency = line.Frequency,
                Duration = line.Duration,
                Instructions = line.Instructions
            });
        }
        return list;
    }

    private static MedicalRecordFormViewModel MapToForm(MedicalRecord r) => new()
    {
        Id = r.Id,
        PatientId = r.PatientId,
        DoctorId = r.DoctorId,
        AppointmentId = r.AppointmentId,
        VisitDate = r.VisitDate,
        ChiefComplaint = r.ChiefComplaint,
        Diagnosis = r.Diagnosis,
        TreatmentPlan = r.TreatmentPlan,
        ClinicalNotes = r.ClinicalNotes,
        Observations = r.Observations,
        HeightCm = r.HeightCm,
        WeightKg = r.WeightKg,
        BloodPressure = r.BloodPressure,
        HeartRateBpm = r.HeartRateBpm,
        TemperatureCelsius = r.TemperatureCelsius,
        PrescriptionLines = r.Prescriptions.Count == 0
            ? [new PrescriptionLineViewModel()]
            : r.Prescriptions.Select(p => new PrescriptionLineViewModel
            {
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Frequency = p.Frequency,
                Duration = p.Duration,
                Instructions = p.Instructions
            }).ToList()
    };
}
