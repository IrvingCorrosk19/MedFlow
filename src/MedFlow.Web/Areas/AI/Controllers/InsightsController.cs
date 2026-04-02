using System.Text;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.AI.Controllers;

[Area("AI")]
[Authorize]
[RequirePermission(PermissionCodes.AIInsightsView)]
public class InsightsController : Controller
{
    private readonly IAIInsightService _insightService;
    private readonly ITenantContext _tenant;
    private readonly IPermissionChecker _permissionChecker;

    public InsightsController(IAIInsightService insightService, ITenantContext tenant, IPermissionChecker permissionChecker)
    {
        _insightService = insightService;
        _tenant = tenant;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(
        AIInsightType? type, AIInsightStatus? status, AISeverity? severity,
        DateTime? from, DateTime? to, decimal? minScore, decimal? minConfidence, string? entityType, string? entityId,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        // Clamp score filters to valid range [0, 1]
        if (minScore.HasValue) minScore = Math.Clamp(minScore.Value, 0.0m, 1.0m);
        if (minConfidence.HasValue) minConfidence = Math.Clamp(minConfidence.Value, 0.0m, 1.0m);

        // Ensure from <= to
        if (from.HasValue && to.HasValue && from > to)
            from = to.Value.AddDays(-30);

        ViewData["Title"] = "Insights de IA";
        ViewData["PageSubtitle"] = "Feed de inferencias y alertas";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"AIDashboard\" asp-action=\"Index\">IA</a></li><li class=\"breadcrumb-item active\">Insights</li>";

        try
        {
            var filter = new AIInsightFilter(_tenant.TenantId.Value, type, status, severity, from, to, minScore, minConfidence, entityType, entityId, page, Math.Clamp(pageSize, 10, 200));
            var list = await _insightService.ListAsync(filter, ct);
            var metrics = await _insightService.GetDashboardMetricsAsync(_tenant.TenantId.Value, DateTime.UtcNow.AddDays(-7), null, ct);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AIInsightsManage, ct);
            ViewBag.Metrics = metrics;
            ViewBag.Type = type; ViewBag.Status = status; ViewBag.Severity = severity;
            ViewBag.From = from; ViewBag.To = to; ViewBag.MinScore = minScore; ViewBag.MinConfidence = minConfidence;
            ViewBag.EntityType = entityType; ViewBag.EntityId = entityId; ViewBag.Page = page; ViewBag.PageSize = pageSize;
            return View(list);
        }
        catch (Exception)
        {
            ViewData["ErrorMessage"] = "Error al cargar los insights.";
            return View(null);
        }
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var insight = await _insightService.GetByIdAsync(id, ct);
        if (insight == null)
            return NotFound();

        if (_tenant.TenantId.HasValue && insight.TenantId != _tenant.TenantId.Value)
            return Forbid();

        ViewData["Title"] = insight.Title;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"AIDashboard\" asp-action=\"Index\">IA</a></li><li class=\"breadcrumb-item\"><a asp-action=\"Index\">Insights</a></li><li class=\"breadcrumb-item active\">Detalle</li>";

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AIInsightsManage, ct);
        return View(insight);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AIInsightsView)]
    public async Task<IActionResult> ExportCsv(
        AIInsightType? type, AIInsightStatus? status, AISeverity? severity,
        DateTime? from, DateTime? to, decimal? minScore, decimal? minConfidence, string? entityType, string? entityId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        if (minScore.HasValue) minScore = Math.Clamp(minScore.Value, 0.0m, 1.0m);
        if (minConfidence.HasValue) minConfidence = Math.Clamp(minConfidence.Value, 0.0m, 1.0m);
        if (from.HasValue && to.HasValue && from > to)
            from = to.Value.AddDays(-30);

        // Fetch all matching records (no pagination for export)
        var filter = new AIInsightFilter(_tenant.TenantId.Value, type, status, severity, from, to, minScore, minConfidence, entityType, entityId, Page: 1, PageSize: 2000);
        var list = await _insightService.ListAsync(filter, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Tipo,Título,Puntaje,Confianza,Estado,Fecha");

        foreach (var insight in list)
        {
            var tipo = EscapeCsvField(insight.InsightType.ToString());
            var titulo = EscapeCsvField(insight.Title);
            var puntaje = insight.Score.HasValue ? insight.Score.Value.ToString("F2") : "";
            var confianza = insight.Confidence.HasValue ? insight.Confidence.Value.ToString("F2") : "";
            var estado = EscapeCsvField(insight.Status.ToString());
            var fecha = insight.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            sb.AppendLine($"{tipo},{titulo},{puntaje},{confianza},{estado},{fecha}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var filename = $"insights_{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv", filename);
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AIInsightsManage)]
    public async Task<IActionResult> Acknowledge(Guid id, string? returnUrl, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        await _insightService.AcknowledgeAsync(id, userId, ct);
        TempData["Success"] = "Insight marcado como revisado.";
        return Redirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/AI/AIDashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AIInsightsManage)]
    public async Task<IActionResult> Dismiss(Guid id, string? returnUrl, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        await _insightService.DismissAsync(id, userId, ct);
        TempData["Success"] = "Insight descartado.";
        return Redirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/AI/AIDashboard");
    }
}
