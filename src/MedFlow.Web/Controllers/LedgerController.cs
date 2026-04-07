using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using MedFlow.Web.Models.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedFlow.Web.Controllers;

[Authorize]
public class LedgerController : Controller
{
    private readonly ILedgerService _ledger;
    private readonly IAccountService _accounts;
    private readonly ITenantContext _tenant;
    private readonly IAccountingExportService _export;
    private readonly IJournalEntryService _journalEntries;
    private readonly IFiscalPeriodService _fiscalPeriods;

    public LedgerController(
        ILedgerService ledger,
        IAccountService accounts,
        ITenantContext tenant,
        IAccountingExportService export,
        IJournalEntryService journalEntries,
        IFiscalPeriodService fiscalPeriods)
    {
        _ledger = ledger;
        _accounts = accounts;
        _tenant = tenant;
        _export = export;
        _journalEntries = journalEntries;
        _fiscalPeriods = fiscalPeriods;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;
        var today = DateTime.Today;
        var firstDay = new DateTime(today.Year, today.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var balanceSheet = await _ledger.GetBalanceSheetAsync(tid, today, ct);
        var incomeStatement = await _ledger.GetIncomeStatementAsync(tid, firstDay, lastDay, ct);
        var recentEntries = await _journalEntries.SearchAsync(tid, today.AddDays(-30), null, null, null, null, ct);
        var allPeriods = await _fiscalPeriods.GetAllAsync(tid, ct);

        var openPeriods = allPeriods
            .Where(p => p.Status == FiscalPeriodStatus.Open)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .ToList();

        var currentPeriod = allPeriods
            .FirstOrDefault(p => p.Year == today.Year && p.Month == today.Month);

        var vm = new AccountingDashboardViewModel
        {
            TotalAssets = balanceSheet.TotalAssets,
            TotalLiabilities = balanceSheet.TotalLiabilities,
            TotalEquity = balanceSheet.TotalEquity,
            TotalRevenue = incomeStatement.TotalRevenue,
            TotalExpenses = incomeStatement.TotalExpenses + incomeStatement.TotalCosts,
            RecentEntries = recentEntries.Take(5).ToList(),
            OpenPeriods = openPeriods,
            CurrentPeriodName = currentPeriod?.Name ?? $"{today:MMMM yyyy}",
        };

        ViewData["Title"] = "Dashboard Contable";
        ViewData["PageSubtitle"] = vm.CurrentPeriodName;
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Dashboard Contable</li>";
        return View(vm);
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(Guid? accountId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var tid = _tenant.TenantId.Value;
        var accountList = await _accounts.GetAllAsync(tid, ct);
        ViewBag.Accounts = new SelectList(
            accountList.Where(a => a.AllowsDirectPosting).Select(a => new { a.Id, Name = $"{a.Code} - {a.Name}" }),
            "Id", "Name", accountId);

        ViewBag.AccountId = accountId;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewData["Title"] = "Libro Mayor";
        ViewData["PageSubtitle"] = "Movimientos por cuenta";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Libro Mayor</li>";

        if (accountId.HasValue)
        {
            var f = from ?? new DateTime(DateTime.Today.Year, 1, 1);
            var t = to ?? DateTime.Today;
            var ledger = await _ledger.GetLedgerAsync(tid, accountId.Value, f, t, ct);
            if (ledger is not null && ledger.Lines.Count > 200)
            {
                ViewBag.Truncated = true;
                var truncatedLines = ledger.Lines.Take(200).ToList();
                ledger = ledger with { Lines = truncatedLines };
            }
            return View(ledger);
        }

        return View(null);
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> TrialBalance(DateTime? asOf, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var date = asOf ?? DateTime.Today;
        var result = await _ledger.GetTrialBalanceAsync(_tenant.TenantId.Value, date, ct);
        ViewBag.AsOf = date.ToString("yyyy-MM-dd");
        ViewData["Title"] = "Balance de Comprobación";
        ViewData["PageSubtitle"] = $"Al {date:dd MMM yyyy}";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Balance de Comprobación</li>";
        return View(result);
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> BalanceSheet(DateTime? asOf, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var date = asOf ?? DateTime.Today;
        var result = await _ledger.GetBalanceSheetAsync(_tenant.TenantId.Value, date, ct);
        ViewBag.AsOf = date.ToString("yyyy-MM-dd");
        ViewData["Title"] = "Balance General";
        ViewData["PageSubtitle"] = $"Al {date:dd MMM yyyy}";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Balance General</li>";
        return View(result);
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> IncomeStatement(DateTime? from, DateTime? to, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var f = from ?? new DateTime(DateTime.Today.Year, 1, 1);
        var t = to ?? DateTime.Today;
        var result = await _ledger.GetIncomeStatementAsync(_tenant.TenantId.Value, f, t, ct);
        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewData["Title"] = "Estado de Resultados";
        ViewData["PageSubtitle"] = $"{f:dd MMM yyyy} - {t:dd MMM yyyy}";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Estado de Resultados</li>";
        return View(result);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> ExportTrialBalance(DateTime? asOf, string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var data = await _ledger.GetTrialBalanceAsync(_tenant.TenantId.Value, asOf ?? DateTime.UtcNow, ct);
        if (format == "html")
        {
            var bytes = _export.ExportTrialBalanceHtml(data);
            return File(bytes, "text/html", $"balance-comprobacion-{(asOf ?? DateTime.UtcNow):yyyy-MM-dd}.html");
        }
        var csv = _export.ExportTrialBalanceCsv(data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"balance-comprobacion-{(asOf ?? DateTime.UtcNow):yyyy-MM-dd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> ExportBalanceSheet(DateTime? asOf, string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var data = await _ledger.GetBalanceSheetAsync(_tenant.TenantId.Value, asOf ?? DateTime.UtcNow, ct);
        if (format == "html")
        {
            var bytes = _export.ExportBalanceSheetHtml(data);
            return File(bytes, "text/html", $"balance-general-{(asOf ?? DateTime.UtcNow):yyyy-MM-dd}.html");
        }
        var csv = _export.ExportBalanceSheetCsv(data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"balance-general-{(asOf ?? DateTime.UtcNow):yyyy-MM-dd}.csv");
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> ExportIncomeStatement(DateTime? from, DateTime? to, string format = "csv", CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var now = DateTime.UtcNow;
        var f = from ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var t = to ?? now;
        var data = await _ledger.GetIncomeStatementAsync(_tenant.TenantId.Value, f, t, ct);
        if (format == "html")
        {
            var bytes = _export.ExportIncomeStatementHtml(data);
            return File(bytes, "text/html", $"estado-resultados-{f:yyyy-MM-dd}_{t:yyyy-MM-dd}.html");
        }
        var csv = _export.ExportIncomeStatementCsv(data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"estado-resultados-{f:yyyy-MM-dd}_{t:yyyy-MM-dd}.csv");
    }
}
