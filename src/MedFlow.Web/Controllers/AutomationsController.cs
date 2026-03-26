using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Application.Security;
using MedFlow.Domain;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.AutomationsView)]
[RequirePlanFeature(PlanFeatureKind.Automation)]
public class AutomationsController : Controller
{
    private readonly IWorkflowDefinitionService _workflows;
    private readonly IWorkflowExecutionService _executions;
    private readonly IPermissionChecker _permissionChecker;

    public AutomationsController(
        IWorkflowDefinitionService workflows,
        IWorkflowExecutionService executions,
        IPermissionChecker permissionChecker)
    {
        _workflows = workflows;
        _executions = executions;
        _permissionChecker = permissionChecker;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Automatizaciones";
        ViewData["PageSubtitle"] = "Workflows desacoplados e integración con n8n";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Automatizaciones</li>";

        var list = await _workflows.ListByTenantAsync(ct);
        var metrics = await _executions.GetMetricsAsync(null, ct);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.CanManage = !string.IsNullOrEmpty(userId) && await _permissionChecker.UserHasPermissionAsync(userId, PermissionCodes.AutomationsManage, ct);
        ViewBag.Metrics = metrics;
        ViewBag.TriggerEvents = WorkflowTriggerEvents.All;
        return View(list);
    }

    [RequirePermission(PermissionCodes.AutomationsManage)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo workflow";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-action=\"Index\">Automatizaciones</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
        ViewBag.TriggerEvents = WorkflowTriggerEvents.All;
        return View(new WorkflowFormModel
        {
            HttpMethod = "POST",
            PayloadTemplateJson = "{\n  \"eventType\": \"{{EventType}}\",\n  \"aggregateId\": \"{{AggregateId}}\",\n  \"payload\": {{Payload}}\n}",
            RetryPolicyJson = "{\"maxAttempts\": 3, \"delaySeconds\": 60, \"backoffType\": \"exponential\", \"multiplier\": 2}"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Create(WorkflowFormModel model, CancellationToken ct)
    {
        ViewBag.TriggerEvents = WorkflowTriggerEvents.All;
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.WebhookUrl) || string.IsNullOrWhiteSpace(model.TriggerEvent))
        {
            ModelState.AddModelError(string.Empty, "Nombre, código, evento y URL del webhook son obligatorios.");
            return View(model);
        }

        try
        {
            await _workflows.CreateAsync(new CreateWorkflowDefinitionCommand(
                model.Name,
                model.Code,
                model.Description,
                model.TriggerEvent,
                model.WebhookUrl,
                model.HttpMethod ?? "POST",
                model.HeadersJson,
                model.PayloadTemplateJson ?? "{}",
                model.RetryPolicyJson,
                model.TimeoutSeconds), ct);
            TempData["Success"] = "Workflow creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var w = await _workflows.GetByIdAsync(id, ct);
        if (w == null) return NotFound();

        ViewData["Title"] = $"Editar: {w.Name}";
        ViewData["Breadcrumb"] = $"<li class=\"breadcrumb-item\"><a asp-action=\"Index\">Automatizaciones</a></li><li class=\"breadcrumb-item active\">Editar</li>";
        ViewBag.TriggerEvents = WorkflowTriggerEvents.All;

        return View(new WorkflowFormModel
        {
            Id = w.Id,
            Name = w.Name,
            Code = w.Code,
            Description = w.Description,
            TriggerEvent = w.TriggerEvent,
            IsActive = w.IsActive,
            WebhookUrl = w.WebhookUrl,
            HttpMethod = w.HttpMethod,
            HeadersJson = w.HeadersJson,
            PayloadTemplateJson = w.PayloadTemplateJson,
            RetryPolicyJson = w.RetryPolicyJson,
            TimeoutSeconds = w.TimeoutSeconds
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Edit(WorkflowFormModel model, CancellationToken ct)
    {
        ViewBag.TriggerEvents = WorkflowTriggerEvents.All;
        if (model.Id == Guid.Empty || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.WebhookUrl))
        {
            ModelState.AddModelError(string.Empty, "Datos incompletos.");
            return View(model);
        }

        try
        {
            await _workflows.UpdateAsync(new UpdateWorkflowDefinitionCommand(
                model.Id,
                model.Name,
                model.Code ?? model.Name.ToLowerInvariant().Replace(" ", "-"),
                model.Description,
                model.TriggerEvent,
                model.IsActive,
                model.WebhookUrl,
                model.HttpMethod ?? "POST",
                model.HeadersJson,
                model.PayloadTemplateJson ?? "{}",
                model.RetryPolicyJson,
                model.TimeoutSeconds), ct);
            TempData["Success"] = "Workflow actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        try
        {
            await _workflows.SetActiveAsync(id, isActive, ct);
            TempData["Success"] = isActive ? "Workflow activado." : "Workflow desactivado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _workflows.DeleteAsync(id, ct);
            TempData["Success"] = "Workflow eliminado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AutomationsManage)]
    public async Task<IActionResult> TestWebhook(Guid id, CancellationToken ct)
    {
        try
        {
            var testSvc = HttpContext.RequestServices.GetRequiredService<IWorkflowTestService>();
            var result = await testSvc.TestWebhookAsync(id, ct);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? $"Webhook respondió OK ({result.StatusCode})."
                : $"Error: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}

public class WorkflowFormModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerEvent { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string WebhookUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string? HeadersJson { get; set; }
    public string? PayloadTemplateJson { get; set; }
    public string? RetryPolicyJson { get; set; }
    public int? TimeoutSeconds { get; set; }
}
