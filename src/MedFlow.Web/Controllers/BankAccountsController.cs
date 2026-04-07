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
public class BankAccountsController : Controller
{
    private readonly IBankAccountService _bankAccounts;
    private readonly IAccountService _accounts;
    private readonly IJournalEntryService _journalEntries;
    private readonly ITenantContext _tenant;

    public BankAccountsController(
        IBankAccountService bankAccounts,
        IAccountService accounts,
        IJournalEntryService journalEntries,
        ITenantContext tenant)
    {
        _bankAccounts = bankAccounts;
        _accounts = accounts;
        _journalEntries = journalEntries;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _bankAccounts.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewData["Title"] = "Cuentas Bancarias";
        ViewData["PageSubtitle"] = "Gestión y conciliación bancaria";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Cuentas Bancarias</li>";
        return View(list);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateAccountSelectAsync(ct);
        ViewData["Title"] = "Nueva Cuenta Bancaria";
        return View(new BankAccountFormViewModel { Currency = "USD" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(BankAccountFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateAccountSelectAsync(ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new BankAccount
        {
            TenantId = tenantId,
            Name = model.Name,
            BankName = model.BankName,
            AccountNumber = model.AccountNumber,
            Currency = model.Currency,
            OpeningBalance = model.OpeningBalance,
            AccountId = model.AccountId,
            IsActive = model.IsActive
        };
        await _bankAccounts.CreateAsync(entity, ct);
        TempData["Success"] = "Cuenta bancaria creada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var account = await _bankAccounts.GetByIdAsync(id, _tenant.TenantId.Value, ct);
        if (account is null) return NotFound();
        await PopulateAccountSelectAsync(ct);
        ViewData["Title"] = $"Editar - {account.Name}";
        var vm = new BankAccountFormViewModel
        {
            Id = account.Id,
            Name = account.Name,
            BankName = account.BankName,
            AccountNumber = account.AccountNumber,
            Currency = account.Currency,
            OpeningBalance = account.OpeningBalance,
            AccountId = account.AccountId,
            IsActive = account.IsActive
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(BankAccountFormViewModel model, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateAccountSelectAsync(ct);
            return View(model);
        }
        var tenantId = _tenant.TenantId.Value;
        var entity = new BankAccount
        {
            Id = model.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = model.Name,
            BankName = model.BankName,
            AccountNumber = model.AccountNumber,
            Currency = model.Currency,
            OpeningBalance = model.OpeningBalance,
            AccountId = model.AccountId,
            IsActive = model.IsActive
        };
        await _bankAccounts.UpdateAsync(entity, ct);
        TempData["Success"] = "Cuenta bancaria actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var account = await _bankAccounts.GetByIdAsync(id, _tenant.TenantId.Value, ct);
        if (account is null) return NotFound();
        ViewData["Title"] = $"Detalle — {account.Name}";
        return View(account);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _bankAccounts.DeleteAsync(id, ct);
        TempData["Success"] = "Cuenta bancaria eliminada.";
        return RedirectToAction(nameof(Index));
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Transactions(Guid id, DateTime? from, DateTime? to, bool? reconciled, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var account = await _bankAccounts.GetByIdAsync(id, _tenant.TenantId.Value, ct);
        if (account is null) return NotFound();

        var transactions = await _bankAccounts.GetTransactionsAsync(id, from, to, reconciled, ct);
        var postedEntries = await _journalEntries.SearchAsync(
            _tenant.TenantId.Value, null, null,
            MedFlow.Domain.Enums.JournalEntryStatus.Posted, null, null, ct);
        ViewBag.BankAccount = account;
        ViewBag.PostedEntries = postedEntries;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Reconciled = reconciled;
        ViewData["Title"] = $"Transacciones - {account.Name}";
        return View(transactions);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> AddTransaction(Guid bankAccountId, BankTransaction tx, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        tx.BankAccountId = bankAccountId;
        tx.TenantId = _tenant.TenantId.Value;
        await _bankAccounts.AddTransactionAsync(tx, ct);
        TempData["Success"] = "Transacción registrada.";
        return RedirectToAction(nameof(Transactions), new { id = bankAccountId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Reconcile(Guid transactionId, Guid journalEntryId, Guid bankAccountId, CancellationToken ct)
    {
        var (ok, error) = await _bankAccounts.ReconcileAsync(transactionId, journalEntryId, ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Transacción conciliada correctamente.";
        return RedirectToAction(nameof(Transactions), new { id = bankAccountId });
    }

    private async Task PopulateAccountSelectAsync(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return;
        var accounts = await _accounts.GetAllAsync(_tenant.TenantId.Value, ct);
        ViewBag.ChartAccounts = new SelectList(
            accounts.Select(a => new { a.Id, Name = $"{a.Code} - {a.Name}" }),
            "Id", "Name");
    }
}
