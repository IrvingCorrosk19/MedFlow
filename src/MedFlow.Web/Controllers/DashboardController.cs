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

    public async Task<IActionResult> Index(int days = 14, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Dashboard ejecutivo";
        ViewData["PageSubtitle"] = "KPIs, tendencias y operación clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Dashboard</li>";

        var clampedDays = Math.Clamp(days, 1, 365);
        ViewBag.Days = clampedDays;

        try
        {
            var model = await _analytics.GetExecutiveDashboardAsync(new ExecutiveDashboardFilter(clampedDays), cancellationToken);
            return View(model);
        }
        catch (Exception ex)
        {
            ViewData["ErrorMessage"] = "No se pudieron cargar los datos del dashboard. Intente de nuevo en unos momentos.";
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(
                HttpContext.RequestServices.GetRequiredService<ILogger<DashboardController>>(), ex, "Error cargando dashboard");
            return View(null as MedFlow.Application.Reporting.ExecutiveDashboardVm);
        }
    }
}
