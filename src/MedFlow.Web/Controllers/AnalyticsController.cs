using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.ReportsView)]
public class AnalyticsController : Controller
{
    private readonly IAdvancedAnalyticsService _analytics;
    private readonly IHistoricalAnalyticsService _historical;
    private readonly IPeriodComparisonService _comparison;
    private readonly ITenantHealthService _health;
    private readonly IBenchmarkingService _benchmarking;
    private readonly IAnalyticsRebuildService _rebuild;
    private readonly IAnalyticsSettingsService _analyticsSettings;
    private readonly MedFlow.Infrastructure.Analytics.AnalyticsSnapshotProcessorService _processor;
    private readonly ITenantContext _tenant;
    private readonly IPermissionChecker _permissionChecker;

    public AnalyticsController(
        IAdvancedAnalyticsService analytics,
        IHistoricalAnalyticsService historical,
        IPeriodComparisonService comparison,
        ITenantHealthService health,
        IBenchmarkingService benchmarking,
        IAnalyticsRebuildService rebuild,
        IAnalyticsSettingsService analyticsSettings,
        MedFlow.Infrastructure.Analytics.AnalyticsSnapshotProcessorService processor,
        ITenantContext tenant,
        IPermissionChecker permissionChecker)
    {
        _analytics = analytics;
        _historical = historical;
        _comparison = comparison;
        _health = health;
        _benchmarking = benchmarking;
        _rebuild = rebuild;
        _analyticsSettings = analyticsSettings;
        _processor = processor;
        _tenant = tenant;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int days = 30, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 7, 90));
        if (from.HasValue && to.HasValue)
            days = (int)(toDate - fromDate).TotalDays;
        var filter = new AdvancedAnalyticsFilter(_tenant.TenantId, fromDate, toDate, 90);

        var model = await _analytics.GetExecutiveAdvancedDashboardAsync(filter, ct);
        var health = await _health.GetHealthScoreAsync(_tenant.TenantId.Value, ct);
        ViewBag.Health = health;

        ViewData["Title"] = "Dashboard ejecutivo";
        ViewData["PageSubtitle"] = "Tendencias, KPIs históricos y benchmarking";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Dashboard\" asp-action=\"Index\">Dashboard</a></li><li class=\"breadcrumb-item active\">Analítica</li>";
        ViewData["From"] = fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = toDate.ToString("yyyy-MM-dd");
        ViewBag.Days = days;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.SettingsManage, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.SettingsManage)]
    public async Task<IActionResult> Aggregate(DateTime? date, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var d = date ?? DateTime.UtcNow.Date.AddDays(-1);
        await _processor.ProcessTenantForDateAsync(_tenant.TenantId.Value, d, ct);
        TempData["Success"] = $"Snapshot agregado para {d:yyyy-MM-dd}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Trends(DateTime? from, DateTime? to, int days = 30, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 7, 90));

        var apt = await _historical.GetAppointmentsByDayAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var rev = await _historical.GetRevenueByDayAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var canc = await _historical.GetCancellationsTrendAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var noShow = await _historical.GetNoShowTrendAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var newPatients = await _historical.GetNewPatientsTrendAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var wfSuccess = await _historical.GetWorkflowSuccessTrendAsync(_tenant.TenantId.Value, fromDate, toDate, ct);
        var aiTrend = await _historical.GetAIInsightsTrendAsync(_tenant.TenantId.Value, fromDate, toDate, ct);

        var aptComp = await _comparison.CompareAppointmentsAsync(_tenant.TenantId.Value, new PeriodComparisonRequest { Type = PeriodComparisonType.ThisWeekVsLastWeek }, ct);
        var revComp = await _comparison.CompareRevenueAsync(_tenant.TenantId.Value, new PeriodComparisonRequest { Type = PeriodComparisonType.ThisWeekVsLastWeek }, ct);

        ViewData["Title"] = "Tendencias históricas";
        ViewData["PageSubtitle"] = "Evolución operativa, financiera e IA";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Analytics\" asp-action=\"Index\">Analítica</a></li><li class=\"breadcrumb-item active\">Tendencias</li>";
        ViewData["From"] = fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = toDate.ToString("yyyy-MM-dd");
        ViewBag.AptComparison = aptComp;
        ViewBag.RevComparison = revComp;

        var model = new TrendsViewModel(apt, rev, canc, noShow, newPatients, wfSuccess, aiTrend);
        return View(model);
    }

    public async Task<IActionResult> Benchmarking(string? cohort, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var settings = await _analyticsSettings.GetSettingsAsync(_tenant.TenantId.Value, ct);
        if (!settings.BenchmarkingEnabled)
            return View("BenchmarkingDisabled");

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-30);

        var benchmark = await _benchmarking.GetTenantBenchmarkAsync(_tenant.TenantId.Value, fromDate, toDate, cohort, ct);
        var cohorts = await _benchmarking.GetCohortAveragesAsync(cohort ?? "all", fromDate, toDate, ct);

        ViewData["Title"] = "Benchmarking multi-tenant";
        ViewData["PageSubtitle"] = "Comparativa anonimizada frente a cohortes similares";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Analytics\" asp-action=\"Index\">Analítica</a></li><li class=\"breadcrumb-item active\">Benchmarking</li>";
        ViewData["From"] = fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = toDate.ToString("yyyy-MM-dd");
        ViewBag.Cohort = cohort ?? "all";

        var model = new BenchmarkingViewModel(benchmark, cohorts.ToList());
        return View(model);
    }

    public async Task<IActionResult> Snapshots(DateTime? from, DateTime? to, int days = 14, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var toDate = to ?? DateTime.UtcNow.Date;
        var fromDate = from ?? toDate.AddDays(-Math.Clamp(days, 7, 90));

        var filter = new AdvancedAnalyticsFilter(_tenant.TenantId, fromDate, toDate, 90);
        var snapshots = await _analytics.GetDailySnapshotsAsync(filter, ct);
        var jobLogs = await _rebuild.GetRecentJobLogsAsync(_tenant.TenantId, 20, ct);

        ViewData["Title"] = "Snapshots diarios";
        ViewData["PageSubtitle"] = "Histórico de agregaciones y jobs";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Analytics\" asp-action=\"Index\">Analítica</a></li><li class=\"breadcrumb-item active\">Snapshots</li>";
        ViewData["From"] = fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = toDate.ToString("yyyy-MM-dd");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.SettingsManage, ct);

        var model = new SnapshotsViewModel(snapshots, jobLogs);
        return View(model);
    }

    [RequirePermission(PermissionCodes.SettingsManage)]
    public async Task<IActionResult> Settings(CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        var settings = await _analyticsSettings.GetSettingsAsync(_tenant.TenantId.Value, ct);
        ViewData["Title"] = "Configuración de analítica";
        ViewData["PageSubtitle"] = "Habilitar/deshabilitar módulos analíticos";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Analytics\" asp-action=\"Index\">Analítica</a></li><li class=\"breadcrumb-item active\">Configuración</li>";
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.SettingsManage)]
    public async Task<IActionResult> Settings(AnalyticsSettingsDto model, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        await _analyticsSettings.UpdateSettingsAsync(_tenant.TenantId.Value, model, ct);
        TempData["Success"] = "Configuración actualizada.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.SettingsManage)]
    public async Task<IActionResult> Rebuild(DateTime? from, DateTime? to, DateTime? date, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        RebuildResult result;
        if (date.HasValue)
        {
            result = await _rebuild.RebuildTenantForDateAsync(_tenant.TenantId.Value, date.Value, ct);
        }
        else if (from.HasValue && to.HasValue)
        {
            result = await _rebuild.RebuildTenantAsync(_tenant.TenantId.Value, from.Value, to.Value, ct);
        }
        else
        {
            TempData["Error"] = "Indica rango de fechas o fecha única.";
            return RedirectToAction(nameof(Snapshots));
        }

        TempData["Success"] = $"Procesados: {result.SnapshotsProcessed}, errores: {result.Errors}.";
        return RedirectToAction(nameof(Snapshots));
    }
}

public record TrendsViewModel(
    IReadOnlyList<TrendPointVm> Appointments,
    IReadOnlyList<TrendPointVm> Revenue,
    IReadOnlyList<TrendPointVm> Cancellations,
    IReadOnlyList<TrendPointVm> NoShow,
    IReadOnlyList<TrendPointVm> NewPatients,
    IReadOnlyList<TrendPointVm> WorkflowSuccess,
    IReadOnlyList<TrendPointVm> AIInsights);

public record BenchmarkingViewModel(BenchmarkSummaryVm? Benchmark, IReadOnlyList<CohortBenchmarkVm> Cohorts);

public record SnapshotsViewModel(
    IReadOnlyList<SnapshotSummaryVm> Snapshots,
    IReadOnlyList<AnalyticsJobLogVm> JobLogs);
