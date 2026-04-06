using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class MedicalController : Controller
{
    private readonly IMedicalRecordService _medicalRecords;
    private readonly IPatientPortalService _portal;

    public MedicalController(IMedicalRecordService medicalRecords, IPatientPortalService portal)
    {
        _medicalRecords = medicalRecords;
        _portal = portal;
    }

    [HttpGet]
    [Route("historial-medico")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");

        var records = await _medicalRecords.GetHistoryByPatientAsync(patientId.Value, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["Title"] = "Mi Historial Médico";
        return View(records);
    }

    [HttpGet]
    [Route("historial-medico/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");

        var record = await _medicalRecords.GetByIdAsync(id, cancellationToken: ct);
        // Verify this record belongs to the authenticated patient
        if (record == null || record.PatientId != patientId.Value) return NotFound();

        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["Title"] = $"Consulta del {record.VisitDate:dd/MM/yyyy}";
        return View(record);
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
