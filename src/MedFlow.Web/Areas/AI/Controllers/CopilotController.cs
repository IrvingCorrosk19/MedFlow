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
public class CopilotController : Controller
{
    private readonly IOperationalCopilotService _copilot;
    private readonly ITenantContext _tenant;

    public CopilotController(IOperationalCopilotService copilot, ITenantContext tenant)
    {
        _copilot = copilot;
        _tenant = tenant;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Copiloto operativo";
        ViewData["PageSubtitle"] = "Consultas guiadas y asistencia";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"AIDashboard\" asp-action=\"Index\">IA</a></li><li class=\"breadcrumb-item active\">Copiloto</li>";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromForm] string query, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return Json(new { error = "Tenant no identificado" });

        var result = await _copilot.QueryAsync(_tenant.TenantId.Value, query ?? "", ct);
        return Json(new
        {
            summary = result.Summary,
            items = result.Items.Select(i => new { i.Title, i.Description, i.EntityType, i.EntityId, i.ActionUrl }),
            suggestions = result.Suggestions
        });
    }
}
