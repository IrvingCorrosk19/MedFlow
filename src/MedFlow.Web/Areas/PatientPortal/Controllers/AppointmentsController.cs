using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class AppointmentsController : Controller
{
    private readonly IPatientPortalService _portal;

    public AppointmentsController(IPatientPortalService portal) => _portal = portal;

    [HttpGet]
    [Route("citas")]
    public async Task<IActionResult> Upcoming(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");

        var appointments = await _portal.GetUpcomingAppointmentsAsync(patientId.Value, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["Options"] = options;
        return View(appointments);
    }

    [HttpGet]
    [Route("citas/historial")]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");

        var appointments = await _portal.GetAppointmentHistoryAsync(patientId.Value, 50, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        return View(appointments);
    }

    [HttpPost]
    [Route("citas/{id:guid}/confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.AllowAppointmentConfirmation)
        {
            TempData["Error"] = "La confirmación de citas no está permitida.";
            return RedirectToAction("Upcoming");
        }

        var ok = await _portal.ConfirmAppointmentAsync(patientId.Value, id, ct);
        if (ok) TempData["Success"] = "Cita confirmada correctamente.";
        else TempData["Error"] = "No se pudo confirmar la cita.";
        return RedirectToAction("Upcoming");
    }

    [HttpPost]
    [Route("citas/{id:guid}/cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? motivo, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.AllowAppointmentCancellation)
        {
            TempData["Error"] = "La cancelación de citas no está permitida.";
            return RedirectToAction("Upcoming");
        }

        var ok = await _portal.CancelAppointmentAsync(patientId.Value, id, motivo, ct);
        if (ok) TempData["Success"] = "Cita cancelada.";
        else TempData["Error"] = "No se pudo cancelar la cita.";
        return RedirectToAction("Upcoming");
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
