using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class LedgerService : ILedgerService
{
    private readonly IApplicationDbContext _context;

    public LedgerService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerAccountDto?> GetLedgerAsync(
        Guid tenantId, Guid accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var account = await _context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.TenantId == tenantId, ct);
        if (account is null) return null;

        // Opening balance = sum of posted lines before 'from'
        var priorLines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId
                     && l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate < from)
            .ToListAsync(ct);

        var openingBalance = account.Type switch
        {
            AccountType.Asset or AccountType.Expense or AccountType.Cost
                => priorLines.Sum(l => l.Debit) - priorLines.Sum(l => l.Credit),
            _ => priorLines.Sum(l => l.Credit) - priorLines.Sum(l => l.Debit)
        };

        var periodLines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .AsNoTracking()
            .Where(l => l.AccountId == accountId
                     && l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= from
                     && l.JournalEntry.EntryDate <= to)
            .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntry.EntryNumber)
            .ToListAsync(ct);

        var running = openingBalance;
        var ledgerLines = periodLines.Select(l =>
        {
            var delta = account.Type switch
            {
                AccountType.Asset or AccountType.Expense or AccountType.Cost => l.Debit - l.Credit,
                _ => l.Credit - l.Debit
            };
            running += delta;
            return new LedgerLineDto(
                l.JournalEntryId,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.EntryDate,
                l.Description ?? l.JournalEntry.Description,
                l.Debit, l.Credit, running);
        }).ToList();

        return new LedgerAccountDto(
            account.Id, account.Code, account.Name, account.Type,
            openingBalance, ledgerLines, running);
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(
        Guid tenantId, DateTime asOf, CancellationToken ct = default)
    {
        var accounts = await _context.Accounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.AllowsDirectPosting)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var sums = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) })
            .ToListAsync(ct);

        var lines = accounts
            .Select(a =>
            {
                var s = sums.FirstOrDefault(x => x.AccountId == a.Id);
                decimal debit = s?.Debit ?? 0;
                decimal credit = s?.Credit ?? 0;

                // Show balance in the natural column
                decimal debitBal = 0, creditBal = 0;
                var balance = a.Type switch
                {
                    AccountType.Asset or AccountType.Expense or AccountType.Cost => debit - credit,
                    _ => credit - debit
                };
                if (balance >= 0)
                    debitBal = a.Type is AccountType.Asset or AccountType.Expense or AccountType.Cost ? balance : 0;
                else
                    creditBal = Math.Abs(balance);

                // Normalise: debit-normal => debit column, credit-normal => credit column
                if (a.Type is AccountType.Asset or AccountType.Expense or AccountType.Cost)
                    (debitBal, creditBal) = (Math.Max(0, debit - credit), Math.Max(0, credit - debit));
                else
                    (debitBal, creditBal) = (Math.Max(0, debit - credit), Math.Max(0, credit - debit));

                return new TrialBalanceLineDto(a.Code, a.Name, a.Type, debitBal, creditBal);
            })
            .Where(l => l.DebitBalance != 0 || l.CreditBalance != 0)
            .ToList();

        return new TrialBalanceDto(asOf, lines, lines.Sum(l => l.DebitBalance), lines.Sum(l => l.CreditBalance));
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(
        Guid tenantId, DateTime asOf, CancellationToken ct = default)
    {
        var accounts = await _context.Accounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.AllowsDirectPosting)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var sums = await GetSumsUpTo(tenantId, asOf, ct);

        var assets = accounts
            .Where(a => a.Type == AccountType.Asset)
            .Select(a => BuildSection(a, sums))
            .Where(s => s.Balance != 0).ToList();

        var liabilities = accounts
            .Where(a => a.Type == AccountType.Liability)
            .Select(a => BuildSection(a, sums))
            .Where(s => s.Balance != 0).ToList();

        var equity = accounts
            .Where(a => a.Type == AccountType.Equity)
            .Select(a => BuildSection(a, sums))
            .Where(s => s.Balance != 0).ToList();

        // Net income: Revenue - Expenses - Costs
        var netIncome = accounts
            .Where(a => a.Type == AccountType.Revenue || a.Type == AccountType.Expense || a.Type == AccountType.Cost)
            .Sum(a =>
            {
                var s = sums.TryGetValue(a.Id, out var v) ? v : (0m, 0m);
                return a.Type == AccountType.Revenue ? s.Item2 - s.Item1 : s.Item1 - s.Item2;
            });

        if (netIncome != 0)
            equity.Add(new BalanceSheetSectionDto("R/E", "Utilidad del ejercicio", netIncome));

        return new BalanceSheetDto(asOf, assets, liabilities, equity,
            assets.Sum(s => s.Balance),
            liabilities.Sum(s => s.Balance),
            equity.Sum(s => s.Balance));
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var accounts = await _context.Accounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.AllowsDirectPosting
                     && (a.Type == AccountType.Revenue || a.Type == AccountType.Expense || a.Type == AccountType.Cost))
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        var sums = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= from
                     && l.JournalEntry.EntryDate <= to)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) })
            .ToListAsync(ct);

        decimal NetAmount(MedFlow.Domain.Entities.Account a)
        {
            var s = sums.FirstOrDefault(x => x.AccountId == a.Id);
            if (s is null) return 0;
            return a.Type == AccountType.Revenue ? s.Credit - s.Debit : s.Debit - s.Credit;
        }

        var revenue = accounts.Where(a => a.Type == AccountType.Revenue)
            .Select(a => new IncomeStatementLineDto(a.Code, a.Name, NetAmount(a)))
            .Where(l => l.Amount != 0).ToList();

        var costs = accounts.Where(a => a.Type == AccountType.Cost)
            .Select(a => new IncomeStatementLineDto(a.Code, a.Name, NetAmount(a)))
            .Where(l => l.Amount != 0).ToList();

        var expenses = accounts.Where(a => a.Type == AccountType.Expense)
            .Select(a => new IncomeStatementLineDto(a.Code, a.Name, NetAmount(a)))
            .Where(l => l.Amount != 0).ToList();

        var totalRevenue = revenue.Sum(l => l.Amount);
        var totalCosts = costs.Sum(l => l.Amount);
        var totalExpenses = expenses.Sum(l => l.Amount);
        var grossProfit = totalRevenue - totalCosts;
        var netIncome = grossProfit - totalExpenses;

        return new IncomeStatementDto(from, to, revenue, costs, expenses,
            totalRevenue, totalCosts, totalExpenses, grossProfit, netIncome);
    }

    private async Task<Dictionary<Guid, (decimal Debit, decimal Credit)>> GetSumsUpTo(Guid tenantId, DateTime asOf, CancellationToken ct)
    {
        var sums = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(l => l.Debit), Credit = g.Sum(l => l.Credit) })
            .ToListAsync(ct);
        return sums.ToDictionary(s => s.AccountId, s => (Debit: s.Debit, Credit: s.Credit));
    }

    private static BalanceSheetSectionDto BuildSection(
        MedFlow.Domain.Entities.Account a,
        Dictionary<Guid, (decimal Debit, decimal Credit)> sums)
    {
        var found = sums.TryGetValue(a.Id, out var v);
        var debit = found ? v.Debit : 0m;
        var credit = found ? v.Credit : 0m;
        var balance = a.Type switch
        {
            AccountType.Asset or AccountType.Expense or AccountType.Cost => debit - credit,
            _ => credit - debit
        };
        return new BalanceSheetSectionDto(a.Code, a.Name, balance);
    }
}
