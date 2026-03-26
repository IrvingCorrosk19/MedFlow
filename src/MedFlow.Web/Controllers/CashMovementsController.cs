using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.CashView)]
[RequirePlanFeature(PlanFeatureKind.Billing)]
public class CashMovementsController : Controller
{
    private readonly ICashMovementService _cash;

    public CashMovementsController(ICashMovementService cash)
    {
        _cash = cash;
    }

    public async Task<IActionResult> Index(DateTime? day, CancellationToken cancellationToken = default)
    {
        var d = day?.ToUniversalTime().Date ?? DateTime.UtcNow.Date;
        var start = d;
        var end = start.AddDays(1);

        var movements = await _cash.GetByDateRangeAsync(start, end, cancellationToken);
        var (income, expense, adj) = await _cash.GetDayTotalsAsync(d, cancellationToken);

        ViewBag.Day = d.ToString("yyyy-MM-dd");
        ViewBag.Income = income;
        ViewBag.Expense = expense;
        ViewBag.Adjustment = adj;
        ViewBag.Net = income - expense + adj;

        ViewData["Title"] = "Caja";
        ViewData["PageSubtitle"] = "Movimientos de caja";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Caja</li>";
        return View(movements);
    }
}
