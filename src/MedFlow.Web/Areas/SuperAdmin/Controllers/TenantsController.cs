using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : Controller
{
    private readonly ISaasTenantAdminService _saas;
    private readonly ISubscriptionPlanAdminService _plans;

    public TenantsController(ISaasTenantAdminService saas, ISubscriptionPlanAdminService plans)
    {
        _saas = saas;
        _plans = plans;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Clínicas (tenants)";
        ViewData["PageSubtitle"] = "Gestión comercial SaaS";
        return View(await _saas.GetTenantsAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var d = await _saas.GetTenantDetailsAsync(id, cancellationToken);
        if (d == null) return NotFound();
        ViewData["Title"] = d.Name;
        ViewData["PageSubtitle"] = $"{d.Code} · plan y consumo";
        ViewBag.Plans = await _plans.GetAllAsync(cancellationToken);
        return View(d);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nueva clínica";
        ViewBag.Plans = await _plans.GetAllAsync(cancellationToken);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string code, string? email, Guid subscriptionPlanId, bool startWithTrial, CancellationToken cancellationToken)
    {
        var dto = new SaasTenantCreateDto
        {
            Name = name ?? "",
            Code = code ?? "",
            Email = email,
            SubscriptionPlanId = subscriptionPlanId,
            StartWithTrial = startWithTrial
        };
        try
        {
            var id = await _saas.CreateTenantWithSubscriptionAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
            TempData["Success"] = "Clínica creada y plan asignado.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Plans = await _plans.GetAllAsync(cancellationToken);
            ViewData["Title"] = "Nueva clínica";
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _saas.SuspendTenantAsync(id, reason, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
            TempData["Success"] = "Clínica suspendida.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _saas.ActivateTenantAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
            TempData["Success"] = "Clínica reactivada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(Guid id, Guid newPlanId, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await _saas.ChangePlanAsync(id, newPlanId, reason ?? "", User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
            TempData["Success"] = "Plan actualizado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
