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
public class RecommendationsController : Controller
{
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly ITenantContext _tenant;

    public RecommendationsController(IRecommendationEngine recommendationEngine, ITenantContext tenant)
    {
        _recommendationEngine = recommendationEngine;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewData["Title"] = "Recomendaciones";
        ViewData["PageSubtitle"] = "Acciones sugeridas por IA";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a asp-controller=\"AIDashboard\" asp-action=\"Index\">IA</a></li><li class=\"breadcrumb-item active\">Recomendaciones</li>";

        var recommendations = await _recommendationEngine.GenerateRecommendationsAsync(_tenant.TenantId.Value, ct);
        return View(recommendations);
    }
}
