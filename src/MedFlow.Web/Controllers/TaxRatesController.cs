using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Web.Authorization;
using MedFlow.Web.Models.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedFlow.Web.Controllers;

[Authorize]
public class TaxRatesController : Controller
{
    private readonly ITaxRateService _taxRates;
    private readonly IAccountService _accounts;
    private readonly ITenantContext _tenant;

    public TaxRatesController(ITaxRateService taxRates, IAccountService accounts, ITenantContext tenant)
    {
        _taxRates = taxRates;
        _accounts = accounts;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _taxRates.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewData["Title"] = "Tasas de Impuesto";
        ViewData["PageSubtitle"] = "IVA, IGV, ITBMS y otros";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Tasas de Impuesto</li>";
        return View(list);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateTaxAccountsAsync(ct);
        ViewData["Title"] = "Nueva Tasa de Impuesto";
        return View(new TaxRateFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(TaxRateFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateTaxAccountsAsync(ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new TaxRate
        {
            Id = model.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = model.Name,
            Code = model.Code,
            Rate = model.Rate,
            TaxAccountId = model.TaxAccountId,
            IsDefault = model.IsDefault,
            IsActive = model.IsActive
        };
        await _taxRates.CreateAsync(entity, ct);
        TempData["Success"] = "Tasa de impuesto creada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var rate = await _taxRates.GetByIdAsync(id, ct);
        if (rate is null) return NotFound();
        await PopulateTaxAccountsAsync(ct);
        ViewData["Title"] = $"Editar Tasa - {rate.Name}";
        var vm = new TaxRateFormViewModel
        {
            Id = rate.Id,
            Name = rate.Name,
            Code = rate.Code,
            Rate = rate.Rate,
            TaxAccountId = rate.TaxAccountId,
            IsDefault = rate.IsDefault,
            IsActive = rate.IsActive
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(TaxRateFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateTaxAccountsAsync(ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new TaxRate
        {
            Id = model.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = model.Name,
            Code = model.Code,
            Rate = model.Rate,
            TaxAccountId = model.TaxAccountId,
            IsDefault = model.IsDefault,
            IsActive = model.IsActive
        };
        await _taxRates.UpdateAsync(entity, ct);
        TempData["Success"] = "Tasa actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var rate = await _taxRates.GetByIdAsync(id, ct);
        if (rate is null) return NotFound();
        ViewData["Title"] = $"Detalle — {rate.Name}";
        return View(rate);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _taxRates.DeleteAsync(id, ct);
        TempData["Success"] = "Tasa eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateTaxAccountsAsync(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return;
        var accounts = await _accounts.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewBag.TaxAccounts = new SelectList(
            accounts.Select(a => new { a.Id, Name = $"{a.Code} - {a.Name}" }),
            "Id", "Name");
    }
}
