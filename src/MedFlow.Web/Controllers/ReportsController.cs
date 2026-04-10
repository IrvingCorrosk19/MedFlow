using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using MedFlow.Web.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.ReportsView)]
[RequirePlanFeature(PlanFeatureKind.Reports)]
public class ReportsController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;
    private readonly IApplicationDbContext _db;
    private readonly IClinicSettingsService _clinicSettings;
    private readonly ITenantContext _tenant;

    public ReportsController(
        IExecutiveAnalyticsService analytics,
        IApplicationDbContext db,
        IClinicSettingsService clinicSettings,
        ITenantContext tenant)
    {
        _analytics = analytics;
        _db = db;
        _clinicSettings = clinicSettings;
        _tenant = tenant;
    }

    public async Task<IActionResult> Appointments(
        DateTime? from,
        DateTime? to,
        Guid? doctorId,
        string? speciality,
        int? status,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reporte de citas";
        ViewData["PageSubtitle"] = "Consulta y análisis de agenda";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Inicio</a></li><li class=\"breadcrumb-item active\">Reporte citas</li>";

        var vm = await _analytics.GetAppointmentsReportAsync(new AppointmentsReportFilter(from, to, doctorId, speciality, status), cancellationToken);

        await FillDoctorSpecialitySelectsAsync(doctorId, speciality, cancellationToken);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.DoctorId = doctorId;
        ViewBag.Speciality = speciality;
        ViewBag.Status = status;

        return View(vm);
    }

    public async Task<IActionResult> Patients(
        DateTime? from,
        DateTime? to,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Reporte de pacientes";
        ViewData["PageSubtitle"] = "Altas y actividad";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Inicio</a></li><li class=\"breadcrumb-item active\">Reporte pacientes</li>";

        if (from.HasValue && to.HasValue && from > to)
            from = to.Value.AddDays(-30);

        var vm = await _analytics.GetPatientsReportAsync(new PatientsReportFilter(from, to, includeInactive), cancellationToken);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.IncludeInactive = includeInactive;
        return View(vm);
    }

    public async Task<IActionResult> Financial(
        DateTime? from,
        DateTime? to,
        Guid? patientId,
        int? paymentMethod,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reporte financiero";
        ViewData["PageSubtitle"] = "Facturación y cobros";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Inicio</a></li><li class=\"breadcrumb-item active\">Reporte financiero</li>";

        var vm = await _analytics.GetFinancialReportAsync(new FinancialReportFilter(from, to, patientId, paymentMethod), cancellationToken);

        var patients = await _db.Patients.AsNoTracking().OrderBy(p => p.PrimerApellido).Take(500).ToListAsync(cancellationToken);
        ViewBag.Patients = new SelectList(patients, "Id", "NombreCompleto", patientId);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.PatientId = patientId;
        ViewBag.PaymentMethod = paymentMethod;

        return View(vm);
    }

    public async Task<IActionResult> Doctors(
        DateTime? from,
        DateTime? to,
        Guid? doctorId,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reporte de doctores";
        ViewData["PageSubtitle"] = "Productividad por profesional";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Inicio</a></li><li class=\"breadcrumb-item active\">Reporte doctores</li>";

        if (from.HasValue && to.HasValue && from > to)
            from = to.Value.AddDays(-30);

        var vm = await _analytics.GetDoctorsReportAsync(new DoctorsReportFilter(from, to, doctorId), cancellationToken);

        var doctors = await _db.Doctors.AsNoTracking().Where(d => !d.IsDeleted).OrderBy(d => d.LastName).ToListAsync(cancellationToken);
        ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", doctorId);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.DoctorId = doctorId;

        return View(vm);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportAppointmentsCsv(
        DateTime? from,
        DateTime? to,
        Guid? doctorId,
        string? speciality,
        int? status,
        CancellationToken cancellationToken)
    {
        var vm = await _analytics.GetAppointmentsReportAsync(
            new AppointmentsReportFilter(from, to, doctorId, speciality, status),
            cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Fecha,Hora,Paciente,Doctor,Especialidad,Estado,Motivo");
        foreach (var r in vm.Rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(r.Date.ToString("dd/MM/yyyy")),
                CsvField(r.Start.ToString(@"hh\:mm")),
                CsvField(r.PatientName),
                CsvField(r.DoctorName),
                CsvField(r.Speciality),
                CsvField(r.StatusLabel),
                CsvField(r.Reason ?? "")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"citas_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportPatientsCsv(
        DateTime? from,
        DateTime? to,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var vm = await _analytics.GetPatientsReportAsync(
            new PatientsReportFilter(from, to, includeInactive),
            cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Paciente,Registro,Estado,Nº citas");
        foreach (var r in vm.Rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(r.Name),
                CsvField(r.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy")),
                CsvField(r.IsActive ? "Activo" : "Inactivo"),
                CsvField(r.AppointmentCount.ToString())));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"pacientes_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportDoctorsCsv(
        DateTime? from,
        DateTime? to,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var vm = await _analytics.GetDoctorsReportAsync(
            new DoctorsReportFilter(from, to, doctorId),
            cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Doctor,Especialidad,Total citas,Completadas,Canceladas,No show");
        foreach (var r in vm.Rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(r.DoctorName),
                CsvField(r.Speciality ?? ""),
                CsvField(r.TotalAppointments.ToString()),
                CsvField(r.Completed.ToString()),
                CsvField(r.Cancelled.ToString()),
                CsvField(r.NoShow.ToString())));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"doctores_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportFinancialCsv(
        DateTime? from,
        DateTime? to,
        Guid? patientId,
        int? paymentMethod,
        CancellationToken cancellationToken)
    {
        var vm = await _analytics.GetFinancialReportAsync(
            new FinancialReportFilter(from, to, patientId, paymentMethod),
            cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Nº Factura,Emisión,Paciente,Total,Saldo,Estado");
        foreach (var r in vm.Invoices)
        {
            sb.AppendLine(string.Join(",",
                CsvField(r.InvoiceNumber),
                CsvField(r.IssueDate.ToLocalTime().ToString("dd/MM/yyyy")),
                CsvField(r.PatientName),
                CsvField(r.Total.ToString("N2")),
                CsvField(r.Balance.ToString("N2")),
                CsvField(r.StatusLabel)));
        }

        sb.AppendLine();
        sb.AppendLine("Fecha Pago,Paciente,Factura,Monto,Método");
        foreach (var r in vm.Payments)
        {
            sb.AppendLine(string.Join(",",
                CsvField(r.PaymentDate.ToLocalTime().ToString("dd/MM/yyyy")),
                CsvField(r.PatientName),
                CsvField(r.InvoiceNumber),
                CsvField(r.Amount.ToString("N2")),
                CsvField(r.MethodLabel)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"financiero_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportFinancialPdf(
        DateTime? from,
        DateTime? to,
        Guid? patientId,
        int? paymentMethod,
        CancellationToken cancellationToken)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();

        var vm = await _analytics.GetFinancialReportAsync(
            new FinancialReportFilter(from, to, patientId, paymentMethod),
            cancellationToken);

        var settings = await _clinicSettings.GetAsync(_tenant.TenantId.Value, cancellationToken);

        var doc = new FinancialReportPdfDocument(settings.Name, vm, from, to);
        var bytes = doc.GeneratePdf();

        return File(bytes, "application/pdf",
            $"reporte-financiero-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> ExportAppointmentsPdf(
        DateTime? from,
        DateTime? to,
        Guid? doctorId,
        string? speciality,
        int? status,
        CancellationToken cancellationToken)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();

        var vm = await _analytics.GetAppointmentsReportAsync(
            new AppointmentsReportFilter(from, to, doctorId, speciality, status),
            cancellationToken);

        var settings = await _clinicSettings.GetAsync(_tenant.TenantId.Value, cancellationToken);

        var doc = new AppointmentsReportPdfDocument(settings.Name, vm, from, to);
        var bytes = doc.GeneratePdf();

        return File(bytes, "application/pdf",
            $"reporte-citas-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    private static string CsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private async Task FillDoctorSpecialitySelectsAsync(Guid? doctorId, string? speciality, CancellationToken cancellationToken)
    {
        var doctors = await _db.Doctors.AsNoTracking().Where(d => !d.IsDeleted && d.IsActive)
            .OrderBy(d => d.LastName).ToListAsync(cancellationToken);
        ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", doctorId);

        var specs = await _db.Doctors.AsNoTracking()
            .Where(d => !d.IsDeleted && d.Speciality != null && d.Speciality != "")
            .Select(d => d.Speciality!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
        ViewBag.SpecialityList = specs;
        ViewBag.SelectedSpeciality = speciality;
    }
}
