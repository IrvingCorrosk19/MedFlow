using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePlanFeature(PlanFeatureKind.Billing)]
public class CashMovementsController : Controller
{
    private readonly ICashMovementService _cash;
    private readonly ITenantContext _tenant;

    public CashMovementsController(ICashMovementService cash, ITenantContext tenant)
    {
        _cash = cash;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.CashView)]
    public async Task<IActionResult> Index(DateTime? day, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        DateTime start, end;
        if (from.HasValue || to.HasValue)
        {
            start = (from ?? to!.Value).ToUniversalTime().Date;
            end = (to ?? from!.Value).ToUniversalTime().Date.AddDays(1);
            if (start > end) start = end.AddDays(-7);
            ViewBag.Day = null;
            ViewBag.From = start.ToString("yyyy-MM-dd");
            ViewBag.To = end.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.IsRange = true;
        }
        else
        {
            var d = day?.ToUniversalTime().Date ?? DateTime.UtcNow.Date;
            start = d;
            end = start.AddDays(1);
            ViewBag.Day = d.ToString("yyyy-MM-dd");
            ViewBag.From = null;
            ViewBag.To = null;
            ViewBag.IsRange = false;
        }

        var movements = await _cash.GetByDateRangeAsync(start, end, cancellationToken);
        var (income, expense, adj) = ViewBag.IsRange
            ? await _cash.GetRangeTotalsAsync(start, end, cancellationToken)
            : await _cash.GetDayTotalsAsync(start, cancellationToken);

        ViewBag.Income = income;
        ViewBag.Expense = expense;
        ViewBag.Adjustment = adj;
        ViewBag.Net = income - expense + adj;

        ViewData["Title"] = "Caja";
        ViewData["PageSubtitle"] = "Movimientos de caja";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Caja</li>";
        return View(movements);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CashCreate)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Nuevo movimiento";
        ViewData["PageSubtitle"] = "Registrar ingreso, egreso o ajuste";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Caja</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
        return View(new CashMovementFormViewModel { MovementDate = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.CashCreate)]
    public async Task<IActionResult> Create(CashMovementFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Nuevo movimiento";
            ViewData["PageSubtitle"] = "Registrar ingreso, egreso o ajuste";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Caja</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        var tenantId = _tenant.TenantId;
        if (!tenantId.HasValue)
            return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var movement = new MedFlow.Domain.Entities.CashMovement
        {
            TenantId = tenantId.Value,
            MovementType = model.MovementType,
            Amount = model.Amount,
            Description = model.Description,
            MovementDate = model.MovementDate,
            CreatedByUserId = userId
        };

        var (created, err) = await _cash.CreateAsync(movement, cancellationToken);
        if (err != null)
        {
            ModelState.AddModelError(string.Empty, err);
            ViewData["Title"] = "Nuevo movimiento";
            ViewData["PageSubtitle"] = "Registrar ingreso, egreso o ajuste";
            ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Caja</a></li><li class=\"breadcrumb-item active\">Nuevo</li>";
            return View(model);
        }

        TempData["Success"] = "Movimiento registrado correctamente.";
        return RedirectToAction(nameof(Index), new { day = created!.MovementDate.ToLocalTime().ToString("yyyy-MM-dd") });
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CashView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var movement = await _cash.GetByIdAsync(id, cancellationToken);
        if (movement == null) return NotFound();

        ViewData["Title"] = "Movimiento de caja";
        ViewData["PageSubtitle"] = movement.MovementDate.ToLocalTime().ToString("dd/MM/yyyy");
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"" + Url.Action(nameof(Index)) + "\">Caja</a></li><li class=\"breadcrumb-item active\">Detalle</li>";
        return View(movement);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.CashDelete)]
    public async Task<IActionResult> Delete(Guid id, string? day, string? from, string? to, CancellationToken cancellationToken = default)
    {
        var deleted = await _cash.DeleteAsync(id, cancellationToken);
        if (deleted)
            TempData["Success"] = "Movimiento eliminado.";
        else
            TempData["Error"] = "No se encontró el movimiento.";

        if (from != null || to != null)
            return RedirectToAction(nameof(Index), new { from, to });

        return RedirectToAction(nameof(Index), new { day });
    }
}
