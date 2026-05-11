using System.Linq;
using System.Security.Claims;
using System.Text;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Domain;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.DashboardView)]
public class RevenueRecoveryController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IWorkflowExecutionService _workflowExecutions;

    public RevenueRecoveryController(
        IExecutiveAnalyticsService analytics,
        IPermissionChecker permissionChecker,
        IWorkflowExecutionService workflowExecutions)
    {
        _analytics = analytics;
        _permissionChecker = permissionChecker;
        _workflowExecutions = workflowExecutions;
    }

    private async Task<bool> UserCanViewFinancialAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;
        return await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.BillingView, cancellationToken);
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Recuperación de ingresos";
        ViewData["PageSubtitle"] = "Cobros, reactivación de pacientes y automatización";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Mission Control</a></li><li class=\"breadcrumb-item active\">Recuperación</li>";

        var showFinancial = await UserCanViewFinancialAsync(cancellationToken);
        ViewBag.ShowFinancialDashboard = showFinancial;

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.CanSeeWorkflowHistory = !string.IsNullOrEmpty(uid) &&
            await _permissionChecker.UserHasPermissionAsync(uid, PermissionCodes.AutomationsView, cancellationToken);

        var fromUtc = DateTime.UtcNow.AddDays(-30);
        var recoveryEventTypes = (IReadOnlyList<string>)
        [
            WorkflowTriggerEvents.InvoiceOverdue,
            WorkflowTriggerEvents.PatientInactive,
            WorkflowTriggerEvents.PatientReengagementSuggested,
            WorkflowTriggerEvents.InvoicePending,
            WorkflowTriggerEvents.NoShowRiskDetected,
            WorkflowTriggerEvents.PaymentRiskDetected,
        ];
        var recoveryCounts = await _workflowExecutions.CountSucceededByEventTypesSinceAsync(recoveryEventTypes, fromUtc, cancellationToken);
        ViewBag.RecoveryWorkflowCounts = recoveryCounts;
        ViewBag.RecoveryWorkflowTotalSucceeded = recoveryCounts.Values.Sum();

        var model = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(30), cancellationToken);

        if (showFinancial && model.FinanceKpis.TotalOutstanding > 0)
        {
            // Heurística orientativa (no contabilidad): ~12% de cartera recuperable con gestión activa.
            ViewBag.EstimatedRecoverableHint = Math.Round(model.FinanceKpis.TotalOutstanding * 0.12m, 2);
        }

        return View(model);
    }

    /// <summary>CSV de atribución workflow recovery (30 d, por tipo de evento).</summary>
    public async Task<IActionResult> ExportWorkflowMetricsCsv(CancellationToken cancellationToken = default)
    {
        var fromUtc = DateTime.UtcNow.AddDays(-30);
        var recoveryEventTypes = (IReadOnlyList<string>)
        [
            WorkflowTriggerEvents.InvoiceOverdue,
            WorkflowTriggerEvents.PatientInactive,
            WorkflowTriggerEvents.PatientReengagementSuggested,
            WorkflowTriggerEvents.InvoicePending,
            WorkflowTriggerEvents.NoShowRiskDetected,
            WorkflowTriggerEvents.PaymentRiskDetected,
        ];
        var recoveryCounts = await _workflowExecutions.CountSucceededByEventTypesSinceAsync(recoveryEventTypes, fromUtc, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("EventType,SucceededLast30Days");
        foreach (var kv in recoveryCounts.OrderByDescending(static x => x.Value))
            sb.AppendLine($"{kv.Key},{kv.Value}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"recovery_workflows_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
