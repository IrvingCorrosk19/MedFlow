using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.AutomationsView)]
[RequirePlanFeature(PlanFeatureKind.Automation)]
public class WorkflowExecutionsController : Controller
{
    private readonly IWorkflowExecutionService _executions;
    private readonly IWorkflowDefinitionService _definitions;
    private readonly IPermissionChecker _permissionChecker;

    public WorkflowExecutionsController(
        IWorkflowExecutionService executions,
        IWorkflowDefinitionService definitions,
        IPermissionChecker permissionChecker)
    {
        _executions = executions;
        _definitions = definitions;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(
        Guid? workflowId,
        WorkflowExecutionStatus? status,
        string? eventType,
        DateTime? from,
        DateTime? to,
        int page = 1,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Ejecuciones de workflows";
        ViewData["PageSubtitle"] = "Trazabilidad y estado";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"Automations\" asp-action=\"Index\">Automatizaciones</a></li><li class=\"breadcrumb-item active\">Ejecuciones</li>";

        var filter = new WorkflowExecutionListFilter(workflowId, status, eventType, page, 50, from?.ToUniversalTime(), to?.ToUniversalTime());
        var list = await _executions.ListAsync(filter, ct);
        var metrics = await _executions.GetMetricsAsync(new WorkflowMetricsFilter(workflowId, from?.ToUniversalTime(), to?.ToUniversalTime()), ct);

        var allDefinitions = await _definitions.ListByTenantAsync(ct);
        ViewBag.Automations = new SelectList(allDefinitions, "Id", "Name", workflowId);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AutomationsManage, ct);
        ViewBag.WorkflowId = workflowId;
        ViewBag.Status = status;
        ViewBag.EventType = eventType;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
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

    [HttpGet]
    [RequirePermission(PermissionCodes.AutomationsView)]
    public async Task<IActionResult> DownloadLog(Guid id, CancellationToken ct = default)
    {
        var exec = await _executions.GetByIdAsync(id, ct);
        if (exec == null) return NotFound();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Ejecución de Workflow ===");
        sb.AppendLine($"ID:            {exec.Id}");
        sb.AppendLine($"Workflow:      {exec.WorkflowDefinition?.Name ?? exec.WorkflowDefinitionId.ToString()}");
        sb.AppendLine($"Evento:        {exec.EventType}");
        sb.AppendLine($"Agregado:      {exec.AggregateId ?? "-"}");
        sb.AppendLine($"Estado:        {exec.Status}");
        sb.AppendLine($"Intentos:      {exec.AttemptCount} / {exec.MaxAttempts}");
        sb.AppendLine($"Iniciado:      {(exec.StartedAt != default ? exec.StartedAt.ToLocalTime().ToString("O") : "-")}");
        sb.AppendLine($"Completado:    {(exec.CompletedAt.HasValue ? exec.CompletedAt.Value.ToLocalTime().ToString("O") : "-")}");
        sb.AppendLine($"Último intento:{(exec.LastAttemptAt.HasValue ? exec.LastAttemptAt.Value.ToLocalTime().ToString("O") : "-")}");
        if (exec.ResponseStatusCode.HasValue)
            sb.AppendLine($"HTTP Status:   {exec.ResponseStatusCode}");

        if (!string.IsNullOrWhiteSpace(exec.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine("=== Error ===");
            sb.AppendLine(exec.ErrorMessage);
        }

        if (!string.IsNullOrWhiteSpace(exec.PayloadJson))
        {
            sb.AppendLine();
            sb.AppendLine("=== Payload enviado ===");
            sb.AppendLine(exec.PayloadJson);
        }

        if (!string.IsNullOrWhiteSpace(exec.ResponseBody))
        {
            sb.AppendLine();
            sb.AppendLine("=== Respuesta del webhook ===");
            sb.AppendLine(exec.ResponseBody);
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/plain", $"execution_{id:N}.log");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Retry(Guid id, string? returnUrl, CancellationToken ct)
    {
        var exec = await _executions.GetByIdAsync(id, ct);
        if (exec == null)
        {
            TempData["Error"] = "Ejecución no encontrada.";
            return Redirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/Automations");
        }

        if (exec.Status != WorkflowExecutionStatus.Failed)
        {
            TempData["Error"] = "Solo se pueden reintentar ejecuciones en estado Fallido.";
            return Redirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/Automations");
        }

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
