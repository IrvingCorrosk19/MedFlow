using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class PlansController : Controller
{
    private readonly ISubscriptionPlanAdminService _plans;

    public PlansController(ISubscriptionPlanAdminService plans) => _plans = plans;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Planes SaaS";
        ViewData["PageSubtitle"] = "Catálogo comercial";
        return View(await _plans.GetAllAsync(cancellationToken));
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo plan";
        return View(new SubscriptionPlanEditDto { Currency = "USD", TrialDays = 14, SortOrder = 100 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionPlanEditDto model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(string.Empty, "Nombre y código son obligatorios.");
        }

        if (!ModelState.IsValid) return View(model);
        try
        {
            var id = await _plans.CreateAsync(model, cancellationToken);
            TempData["Success"] = "Plan creado.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var p = await _plans.GetForEditAsync(id, cancellationToken);
        if (p == null) return NotFound();
        ViewData["Title"] = "Editar plan";
        return View(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SubscriptionPlanEditDto model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(string.Empty, "Nombre y código son obligatorios.");
        }

        if (!ModelState.IsValid) return View(model);
        try
        {
            await _plans.UpdateAsync(id, model, cancellationToken);
            TempData["Success"] = "Plan actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
