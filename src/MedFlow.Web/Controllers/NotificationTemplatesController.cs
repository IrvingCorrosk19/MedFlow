using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class NotificationTemplatesController : Controller
{
    private readonly ITenantContext _tenant;
    private readonly INotificationTemplateService _templates;
    private readonly INotificationPreferenceService _prefs;

    public NotificationTemplatesController(ITenantContext tenant, INotificationTemplateService templates, INotificationPreferenceService prefs)
    {
        _tenant = tenant;
        _templates = templates;
        _prefs = prefs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        ViewData["Title"] = "Plantillas de notificaciones";
        ViewData["PageSubtitle"] = "Email, in-app, webhook";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/Settings\">Configuración</a></li><li class=\"breadcrumb-item active\">Notificaciones</li>";

        var list = await _templates.GetByTenantAsync(tenantId.Value, ct);
        ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
        ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
        return View(list);
    }

    public IActionResult Create(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        ViewData["Title"] = "Nueva plantilla";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/Settings\">Configuración</a></li><li class=\"breadcrumb-item\"><a href=\"/NotificationTemplates\">Notificaciones</a></li><li class=\"breadcrumb-item active\">Nueva</li>";
        ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
        ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
        return View(new NotificationTemplateEditDto(NotificationEventType.AppointmentReminder, NotificationChannel.Email, "", "", null, null, null, null, null, null, null, "POST", null, null, false, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NotificationTemplateEditDto dto, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        {
            ModelState.AddModelError(string.Empty, "Código y nombre son obligatorios.");
            ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
            ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
            return View(dto);
        }

        try
        {
            await _templates.CreateAsync(tenantId.Value, dto, ct);
            TempData["Success"] = "Plantilla creada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
            ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var t = await _templates.GetByIdAsync(id, ct);
        if (t == null) return NotFound();

        ViewData["Title"] = "Editar plantilla";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/Settings\">Configuración</a></li><li class=\"breadcrumb-item\"><a href=\"/NotificationTemplates\">Notificaciones</a></li><li class=\"breadcrumb-item active\">Editar</li>";
        ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
        ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();

        var dto = new NotificationTemplateEditDto(t.EventType, t.Channel, t.Code, t.Name, t.SubjectTemplate, t.BodyTemplate, t.HtmlBodyTemplate, t.FromEmail, t.FromName, t.ReplyTo, t.WebhookUrl, t.WebhookMethod ?? "POST", t.ResendTemplateId, t.WhatsAppTemplateId, t.IsDefault, t.Description);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, NotificationTemplateEditDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        {
            ModelState.AddModelError(string.Empty, "Código y nombre son obligatorios.");
            ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
            ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
            return View(dto);
        }

        try
        {
            await _templates.UpdateAsync(id, dto, ct);
            TempData["Success"] = "Plantilla actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
            ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _templates.DeleteAsync(id, ct);
            TempData["Success"] = "Plantilla eliminada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Preferences(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        ViewData["Title"] = "Preferencias de notificaciones";
        ViewData["PageSubtitle"] = "Activar o desactivar canales por evento";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/Settings\">Configuración</a></li><li class=\"breadcrumb-item\"><a href=\"/NotificationTemplates\">Notificaciones</a></li><li class=\"breadcrumb-item active\">Preferencias</li>";

        var prefs = await _prefs.GetByTenantAsync(tenantId.Value, ct);
        var templates = await _templates.GetByTenantAsync(tenantId.Value, ct);
        ViewBag.Templates = templates;
        ViewBag.EventTypes = Enum.GetValues<NotificationEventType>().Where(e => e != NotificationEventType.Custom).ToList();
        ViewBag.Channels = Enum.GetValues<NotificationChannel>().ToList();
        return View(prefs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPreference(NotificationPreferenceEditDto dto, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue) return NotFound();

        try
        {
            await _prefs.SetAsync(tenantId.Value, dto, ct);
            TempData["Success"] = "Preferencia actualizada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Preferences));
    }
}
