using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IApplicationDbContext _context;

    public AccountService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AccountListItemDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Parent)
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Code)
            .ToListAsync(ct);

        return accounts.Select(a => ToListItem(a)).ToList();
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Accounts
            .Include(a => a.Parent)
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Account> CreateAsync(Account account, CancellationToken ct = default)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(ct);
        return account;
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        var existing = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");
        existing.Code = account.Code;
        existing.Name = account.Name;
        existing.Description = account.Description;
        existing.Type = account.Type;
        existing.ParentId = account.ParentId;
        existing.AllowsDirectPosting = account.AllowsDirectPosting;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");
        account.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateNextCodeAsync(Guid tenantId, string prefix, CancellationToken ct = default)
    {
        var last = await _context.Accounts
            .Where(a => a.TenantId == tenantId && a.Code.StartsWith(prefix))
            .OrderByDescending(a => a.Code)
            .Select(a => a.Code)
            .FirstOrDefaultAsync(ct);

        if (last is null)
            return $"{prefix}00";

        if (int.TryParse(last.Replace(prefix, ""), out var num))
            return $"{prefix}{(num + 1):D2}";

        return $"{prefix}01";
    }

    public async Task RecalculateBalancesAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Gather sums from posted journal entry lines
        var sums = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.TenantId == tenantId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(l => l.Debit),
                TotalCredit = g.Sum(l => l.Credit)
            })
            .ToListAsync(ct);

        var accounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var account in accounts)
        {
            var sum = sums.FirstOrDefault(s => s.AccountId == account.Id);
            if (sum is null)
            {
                account.CurrentBalance = 0;
                continue;
            }

            // Debit-normal accounts: Asset, Expense, Cost
            account.CurrentBalance = account.Type switch
            {
                AccountType.Asset or AccountType.Expense or AccountType.Cost
                    => sum.TotalDebit - sum.TotalCredit,
                _ => sum.TotalCredit - sum.TotalDebit
            };
        }

        await _context.SaveChangesAsync(ct);
    }

    private static AccountListItemDto ToListItem(Account a)
    {
        static string TypeLabel(AccountType t) => t switch
        {
            AccountType.Asset => "Activo",
            AccountType.Liability => "Pasivo",
            AccountType.Equity => "Patrimonio",
            AccountType.Revenue => "Ingreso",
            AccountType.Expense => "Gasto",
            AccountType.Cost => "Costo",
            _ => t.ToString()
        };

        int level = 0;
        var p = a.Parent;
        while (p is not null) { level++; p = p.Parent; }

        return new AccountListItemDto(
            a.Id,
            a.Code,
            a.Name,
            a.Type,
            TypeLabel(a.Type),
            a.ParentId,
            a.Parent?.Code,
            a.AllowsDirectPosting,
            a.CurrentBalance,
            level);
    }
}
