using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.AI.Controllers;

[Area("AI")]
[Authorize]
[RequirePermission(PermissionCodes.AIInsightsView)]
public class AIDashboardController : Controller
{
    private readonly IAIInsightService _insightService;
    private readonly IAIInsightProcessorService _processor;
    private readonly IOperationalSummaryService _summaryService;
    private readonly ITenantContext _tenant;
    private readonly IPermissionChecker _permissionChecker;

    public AIDashboardController(IAIInsightService insightService, IAIInsightProcessorService processor, IOperationalSummaryService summaryService, ITenantContext tenant, IPermissionChecker permissionChecker)
    {
        _insightService = insightService;
        _processor = processor;
        _summaryService = summaryService;
        _tenant = tenant;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewData["Title"] = "Dashboard de IA";
        ViewData["PageSubtitle"] = "Insights operativos y recomendaciones inteligentes";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">IA</li>";

        try
        {
            var metrics = await _insightService.GetDashboardMetricsAsync(_tenant.TenantId.Value, DateTime.UtcNow.AddDays(-30), null, ct);
            var todaySummary = await _summaryService.GenerateDailySummaryAsync(_tenant.TenantId.Value, DateTime.UtcNow.Date, ct);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AIInsightsManage, ct);
            ViewBag.Metrics = metrics;
            ViewBag.TodaySummary = todaySummary;
            return View();
        }
        catch (Exception)
        {
            ViewData["ErrorMessage"] = "Error al cargar los datos del dashboard de IA.";
            ViewBag.Metrics = null;
            ViewBag.TodaySummary = null;
            return View();
        }
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
            TempData["Success"] = "Procesamiento de IA ejecutado. Los insights se actualizarán en breve.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al procesar: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
