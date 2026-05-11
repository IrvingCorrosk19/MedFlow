using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

/// <summary>Consola SaaS del tenant: uso vs límites del plan.</summary>
[Authorize]
[RequirePermission(PermissionCodes.SettingsManage)]
public class ClinicConsoleController : Controller
{
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionLimitService _limits;

    public ClinicConsoleController(ITenantContext tenant, ISubscriptionLimitService limits)
    {
        _tenant = tenant;
        _limits = limits;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_tenant.TenantId.HasValue)
            return NotFound();

        ViewData["Title"] = "Consola clínica";
        ViewData["PageSubtitle"] = "Uso, límites y módulos del plan";
        ViewData["Breadcrumb"] =
            "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action("Index", "Dashboard") + "\">Mission Control</a></li><li class=\"breadcrumb-item active\">Consola clínica</li>";

        var usage = await _limits.GetCurrentUsageAsync(_tenant.TenantId.Value, cancellationToken);
        return View(usage);
    }
}
