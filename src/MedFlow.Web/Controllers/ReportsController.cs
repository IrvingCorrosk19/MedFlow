using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.ReportsView)]
[RequirePlanFeature(PlanFeatureKind.Reports)]
public class ReportsController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;
    private readonly IApplicationDbContext _db;

    public ReportsController(IExecutiveAnalyticsService analytics, IApplicationDbContext db)
    {
        _analytics = analytics;
        _db = db;
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

        var vm = await _analytics.GetDoctorsReportAsync(new DoctorsReportFilter(from, to, doctorId), cancellationToken);

        var doctors = await _db.Doctors.AsNoTracking().Where(d => !d.IsDeleted).OrderBy(d => d.LastName).ToListAsync(cancellationToken);
        ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", doctorId);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.DoctorId = doctorId;

        return View(vm);
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
