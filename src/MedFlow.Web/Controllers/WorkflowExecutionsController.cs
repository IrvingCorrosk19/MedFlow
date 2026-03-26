using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.AutomationsView)]
[RequirePlanFeature(PlanFeatureKind.Automation)]
public class WorkflowExecutionsController : Controller
{
    private readonly IWorkflowExecutionService _executions;
    private readonly IPermissionChecker _permissionChecker;

    public WorkflowExecutionsController(IWorkflowExecutionService executions, IPermissionChecker permissionChecker)
    {
        _executions = executions;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(Guid? workflowId, WorkflowExecutionStatus? status, string? eventType, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Ejecuciones de workflows";
        ViewData["PageSubtitle"] = "Trazabilidad y estado";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Automations\" asp-action=\"Index\">Automatizaciones</a></li><li class=\"breadcrumb-item active\">Ejecuciones</li>";

        var filter = new WorkflowExecutionListFilter(workflowId, status, eventType, page, 50);
        var list = await _executions.ListAsync(filter, ct);
        var metrics = await _executions.GetMetricsAsync(new WorkflowMetricsFilter(workflowId), ct);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AutomationsManage, ct);
        ViewBag.WorkflowId = workflowId;
        ViewBag.Status = status;
        ViewBag.EventType = eventType;
        ViewBag.Metrics = metrics;
        ViewBag.Page = page;
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var exec = await _executions.GetByIdAsync(id, ct);
        if (exec == null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AutomationsManage, ct);
        ViewData["Title"] = $"Ejecución {id:N}";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Automations\" asp-action=\"Index\">Automatizaciones</a></li><li class=\"breadcrumb-item\"><a asp-action=\"Index\">Ejecuciones</a></li><li class=\"breadcrumb-item active\">Detalle</li>";
        return View(exec);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Retry(Guid id, string? returnUrl, CancellationToken ct)
    {
        try
        {
            await _executions.RetryAsync(id, ct);
            TempData["Success"] = "Reintento programado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/Automations");
    }
}
