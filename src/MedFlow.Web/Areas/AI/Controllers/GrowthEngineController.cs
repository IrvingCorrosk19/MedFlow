using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.AI.Controllers;

[Area("AI")]
[Authorize]
[RequirePermission(PermissionCodes.AIInsightsView)]
public class GrowthEngineController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;
    private readonly IAIInsightProcessorService _processor;
    private readonly IOperationalSummaryService _summaryService;
    private readonly ITenantContext _tenant;
    private readonly IPermissionChecker _permissionChecker;

    public GrowthEngineController(
        IExecutiveAnalyticsService analytics,
        IAIInsightProcessorService processor,
        IOperationalSummaryService summaryService,
        ITenantContext tenant,
        IPermissionChecker permissionChecker)
    {
        _analytics = analytics;
        _processor = processor;
        _summaryService = summaryService;
        _tenant = tenant;
        _permissionChecker = permissionChecker;
    }

    private async Task<bool> UserCanViewFinancialAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;
        return await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.BillingView, ct);
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewBag.ShowFinancialDashboard = await UserCanViewFinancialAsync(ct);

        ViewData["Title"] = "Motor IA Growth";
        ViewData["PageSubtitle"] = "Resumen ejecutivo + disparadores inteligentes";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "AIDashboard", new { area = "AI" }) + "\">IA</a></li><li class=\"breadcrumb-item active\">Growth Engine</li>";

        ExecutiveDashboardVm? dash = null;
        try
        {
            dash = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(14), ct);
        }
        catch
        {
            ViewData["DashboardError"] = "No se pudieron cargar KPIs del tenant.";
        }

        object? summary = null;
        try
        {
            summary = await _summaryService.GenerateDailySummaryAsync(_tenant.TenantId.Value, DateTime.UtcNow.Date, ct);
        }
        catch
        {
            // opcional: seguir sin resumen
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManageAi = !string.IsNullOrEmpty(userId) &&
            await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AIInsightsManage, ct);
        ViewBag.Dashboard = dash;
        ViewBag.TodaySummary = summary;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AIInsightsManage)]
    public async Task<IActionResult> ProcessNow(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();
        try
        {
            await _processor.ProcessTenantAsync(_tenant.TenantId.Value, ct);
            TempData["Success"] = "Motor de IA ejecutado. Los insights se actualizarán en breve.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al procesar: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
