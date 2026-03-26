using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : Controller
{
    private readonly ISaasSubscriptionQueryService _q;

    public SubscriptionsController(ISaasSubscriptionQueryService q) => _q = q;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Suscripciones";
        ViewData["PageSubtitle"] = "Historial y estado por clínica";
        return View(await _q.GetAllAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var d = await _q.GetDetailsAsync(id, cancellationToken);
        if (d == null) return NotFound();
        ViewData["Title"] = $"Suscripción · {d.TenantCode}";
        ViewData["PageSubtitle"] = d.PlanName;
        return View(d);
    }
}
