using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedFlow.Web.Controllers;

[Authorize]
public class FiscalPeriodsController : Controller
{
    private readonly IFiscalPeriodService _fiscalPeriods;
    private readonly ITenantContext _tenant;

    public FiscalPeriodsController(IFiscalPeriodService fiscalPeriods, ITenantContext tenant)
    {
        _fiscalPeriods = fiscalPeriods;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _fiscalPeriods.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewData["Title"] = "Períodos Fiscales";
        ViewData["PageSubtitle"] = "Gestión de períodos contables";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Períodos Fiscales</li>";
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingClose)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (ok, error) = await _fiscalPeriods.CloseAsync(id, userId, ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Período cerrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingClose)]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (ok, error) = await _fiscalPeriods.ReopenAsync(id, userId, ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Período reabierto.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> CloseYear(int year, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (ok, error) = await _fiscalPeriods.CloseYearAsync(_tenant.TenantId.Value, year, userId, ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = $"Ejercicio fiscal {year} cerrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> CreateOpening(int year, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var (ok, error) = await _fiscalPeriods.CreateOpeningEntryAsync(tenantId, year, userId, ct);
        if (ok)
            TempData["Success"] = $"Asiento de apertura {year} generado correctamente.";
        else
            TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }
}
