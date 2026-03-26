using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.AI.Controllers;

[Area("AI")]
[Authorize]
[RequirePermission(PermissionCodes.AIInsightsManage)]
public class AISettingsController : Controller
{
    private readonly IAISettingsService _aiSettings;
    private readonly ITenantContext _tenant;

    public AISettingsController(IAISettingsService aiSettings, ITenantContext tenant)
    {
        _aiSettings = aiSettings;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewData["Title"] = "Configuración de IA";
        ViewData["PageSubtitle"] = "Activar o desactivar capacidades por tenant";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"AIDashboard\" asp-action=\"Index\">IA</a></li><li class=\"breadcrumb-item active\">Configuración</li>";

        var settings = await _aiSettings.GetSettingsAsync(_tenant.TenantId.Value, ct);
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AITenantSettingsDto model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        await _aiSettings.UpdateSettingsAsync(_tenant.TenantId.Value, model, ct);
        TempData["Success"] = "Configuración guardada.";
        return RedirectToAction(nameof(Index));
    }
}
