using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedFlow.Web.Controllers;

[Authorize(Roles = "Patient")]
[Route("portal")]
public class PatientPortalController : Controller
{
    private readonly IPatientPortalService _portal;
    private readonly ITenantContext _tenant;

    public PatientPortalController(IPatientPortalService portal, ITenantContext tenant)
    {
        _portal = portal;
        _tenant = tenant;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<Guid?> GetPatientIdAsync(CancellationToken ct)
        => await _portal.GetPatientIdByUserIdAsync(CurrentUserId ?? "", ct);

    // ── Dashboard ──────────────────────────────────────────────────────────────

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var vm = await _portal.GetDashboardAsync(patientId.Value, ct);
        if (vm == null) return View("NoAccess");

        ViewData["Title"] = $"Bienvenido, {vm.Profile.FullName.Split(' ').First()}";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View(vm);
    }

    // ── Citas ──────────────────────────────────────────────────────────────────

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var upcoming = await _portal.GetUpcomingAppointmentsAsync(patientId.Value, ct);
        var history  = await _portal.GetAppointmentHistoryAsync(patientId.Value, 50, ct);

        ViewData["Title"] = "Mis Citas";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View((Upcoming: upcoming, History: history));
    }

    [HttpGet("appointments/{id:guid}")]
    public async Task<IActionResult> AppointmentDetails(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var appt = await _portal.GetAppointmentAsync(patientId.Value, id, ct);
        if (appt == null) return NotFound();

        ViewData["Title"] = "Detalle de Cita";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View(appt);
    }

    [HttpPost("appointments/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        await _portal.ConfirmAppointmentAsync(patientId.Value, id, ct);
        TempData["Success"] = "Asistencia confirmada. ¡Te esperamos!";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(Guid id, string? reason, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var ok = await _portal.CancelAppointmentAsync(patientId.Value, id, reason, ct);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Cita cancelada. El consultorio ha sido notificado."
            : "No fue posible cancelar la cita. Contáctanos directamente.";
        return RedirectToAction(nameof(Appointments));
    }

    // ── Facturas ───────────────────────────────────────────────────────────────

    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var invoices = await _portal.GetInvoicesAsync(patientId.Value, ct);
        var (balanceDue, totalPaid) = await _portal.GetAccountStatusAsync(patientId.Value, ct);

        ViewData["Title"] = "Mis Facturas";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View((Invoices: invoices, BalanceDue: balanceDue, TotalPaid: totalPaid));
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> InvoiceDetails(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var invoice = await _portal.GetInvoiceAsync(patientId.Value, id, ct);
        if (invoice == null) return NotFound();

        ViewData["Title"] = $"Factura {invoice.InvoiceNumber}";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View(invoice);
    }

    // ── Notificaciones ─────────────────────────────────────────────────────────

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(int page = 1, CancellationToken ct = default)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var skip = (page - 1) * 20;
        var items = await _portal.GetNotificationsAsync(patientId.Value, skip, 20, ct);

        ViewData["Title"] = "Notificaciones";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        ViewBag.Page = page;
        return View(items);
    }

    [HttpPost("notifications/{id:guid}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return Json(new { ok = false });

        await _portal.MarkNotificationReadAsync(patientId.Value, id, ct);
        return Json(new { ok = true });
    }

    // ── Perfil ─────────────────────────────────────────────────────────────────

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        var profile = await _portal.GetProfileAsync(patientId.Value, ct);
        if (profile == null) return View("NoAccess");

        ViewData["Title"] = "Mi Perfil";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View(profile);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(PatientProfileUpdateDto dto, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue) return View("NoAccess");

        await _portal.UpdateProfileAsync(patientId.Value, dto, ct);
        TempData["Success"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Profile));
    }

    // ── Sin acceso ─────────────────────────────────────────────────────────────

    [HttpGet("no-access")]
    public IActionResult NoAccess()
    {
        ViewData["Title"] = "Sin acceso";
        ViewData["Layout"] = "~/Views/Shared/_PatientLayout.cshtml";
        return View();
    }
}
