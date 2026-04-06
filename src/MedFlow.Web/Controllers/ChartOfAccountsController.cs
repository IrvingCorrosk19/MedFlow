using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.Models.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedFlow.Web.Controllers;

[Authorize]
public class ChartOfAccountsController : Controller
{
    private readonly IAccountService _accounts;
    private readonly ITenantContext _tenant;

    public ChartOfAccountsController(IAccountService accounts, ITenantContext tenant)
    {
        _accounts = accounts;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _accounts.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewData["Title"] = "Plan de Cuentas";
        ViewData["PageSubtitle"] = "Catálogo de cuentas contables";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Plan de Cuentas</li>";
        return View(list);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        await PopulateParentSelectAsync(null, ct);
        ViewData["Title"] = "Nueva Cuenta";
        return View(new AccountFormViewModel { AllowsDirectPosting = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(AccountFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateParentSelectAsync(null, ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new Account
        {
            TenantId = tenantId,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description,
            Type = model.Type,
            ParentId = model.ParentId,
            AllowsDirectPosting = model.AllowsDirectPosting
        };
        await _accounts.CreateAsync(entity, ct);
        TempData["Success"] = $"Cuenta {model.Code} creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(id, ct);
        if (account == null) return NotFound();
        await PopulateParentSelectAsync(id, ct);
        ViewData["Title"] = $"Editar Cuenta {account.Code}";
        var vm = new AccountFormViewModel
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            Description = account.Description,
            Type = account.Type,
            ParentId = account.ParentId,
            AllowsDirectPosting = account.AllowsDirectPosting
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(AccountFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateParentSelectAsync(model.Id, ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new Account
        {
            Id = model.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description,
            Type = model.Type,
            ParentId = model.ParentId,
            AllowsDirectPosting = model.AllowsDirectPosting
        };
        await _accounts.UpdateAsync(entity, ct);
        TempData["Success"] = "Cuenta actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _accounts.DeleteAsync(id, ct);
        TempData["Success"] = "Cuenta eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentSelectAsync(Guid? excludeId, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return;
        var all = await _accounts.GetAllAsync(_tenant.TenantId.Value, ct);
        var filtered = excludeId.HasValue ? all.Where(a => a.Id != excludeId.Value).ToList() : all.ToList();
        ViewBag.ParentAccounts = new SelectList(filtered.Select(a => new { a.Id, Name = $"{a.Code} - {a.Name}" }), "Id", "Name");
    }
}
