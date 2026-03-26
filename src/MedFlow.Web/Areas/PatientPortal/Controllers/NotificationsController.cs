using MedFlow.Application.Interfaces;
using MedFlow.Web.Areas.PatientPortal.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.PatientPortal.Controllers;

[Area("PatientPortal")]
[Route("PatientPortal")]
[PatientPortalAuthorize]
public class NotificationsController : Controller
{
    private readonly IPatientPortalService _portal;

    public NotificationsController(IPatientPortalService portal) => _portal = portal;

    [HttpGet]
    [Route("notificaciones")]
    public async Task<IActionResult> Index(int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        var options = await _portal.GetOptionsAsync(GetTenantId(), ct);
        if (!options.Enabled) return RedirectToAction("Login", "Auth");
        if (!options.ShowNotifications) { TempData["Warning"] = "Las notificaciones no están disponibles."; return RedirectToAction("Index", "Home"); }

        var notifications = await _portal.GetNotificationsAsync(patientId.Value, skip, take, ct);
        var unread = await _portal.GetUnreadNotificationsCountAsync(patientId.Value, ct);
        HttpContext.Items["UnreadNotificationsCount"] = unread;

        ViewData["UnreadCount"] = unread;
        return View(notifications);
    }

    [HttpPost]
    [Route("notificaciones/{id:guid}/leer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, string? returnUrl, CancellationToken ct)
    {
        var patientId = GetPatientId();
        if (!patientId.HasValue) return RedirectToAction("AccessDenied", "Auth");

        await _portal.MarkNotificationReadAsync(patientId.Value, id, ct);
        return Redirect(returnUrl ?? Url.Action("Index") ?? "/");
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
