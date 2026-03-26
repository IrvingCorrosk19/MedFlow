using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.DashboardView)]
public class DashboardController : Controller
{
    private readonly IExecutiveAnalyticsService _analytics;

    public DashboardController(IExecutiveAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(14), cancellationToken);

        ViewData["Title"] = "Dashboard ejecutivo";
        ViewData["PageSubtitle"] = "KPIs, tendencias y operación clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Dashboard</li>";

        return View(model);
    }
}
