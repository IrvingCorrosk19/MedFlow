using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class BankAccountService : IBankAccountService
{
    private readonly IApplicationDbContext _context;

    public BankAccountService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BankAccountDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var accounts = await _context.BankAccounts
            .AsNoTracking()
            .Include(b => b.Account)
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

        var balances = await _context.BankTransactions
            .Where(t => t.TenantId == tenantId)
            .GroupBy(t => t.BankAccountId)
            .Select(g => new
            {
                Id = g.Key,
                Credits = g.Where(t => t.Type == BankTransactionType.Credit).Sum(t => t.Amount),
                Debits = g.Where(t => t.Type == BankTransactionType.Debit).Sum(t => t.Amount)
            })
            .ToListAsync(ct);

        return accounts.Select(b =>
        {
            var bal = balances.FirstOrDefault(x => x.Id == b.Id);
            var current = b.OpeningBalance + (bal?.Credits ?? 0) - (bal?.Debits ?? 0);
            return new BankAccountDto(
                b.Id, b.Name, b.BankName, b.AccountNumber, b.Currency,
                b.OpeningBalance, current, b.AccountId, b.Account?.Code, !b.IsDeleted);
        }).ToList();
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _context.BankAccounts.Include(b => b.Account).FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId, ct);

    public async Task<BankAccount> CreateAsync(BankAccount account, CancellationToken ct = default)
    {
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync(ct);
        return account;
    }

    public async Task UpdateAsync(BankAccount account, CancellationToken ct = default)
    {
        var existing = await _context.BankAccounts.FirstOrDefaultAsync(b => b.Id == account.Id, ct)
            ?? throw new InvalidOperationException("Cuenta bancaria no encontrada.");
        existing.Name = account.Name;
        existing.BankName = account.BankName;
        existing.AccountNumber = account.AccountNumber;
        existing.Currency = account.Currency;
        existing.OpeningBalance = account.OpeningBalance;
        existing.AccountId = account.AccountId;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _context.BankAccounts.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException("Cuenta bancaria no encontrada.");
        account.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BankTransactionDto>> GetTransactionsAsync(
        Guid bankAccountId, DateTime? from, DateTime? to, bool? reconciled, CancellationToken ct = default)
    {
        var q = _context.BankTransactions
            .AsNoTracking()
            .Include(t => t.JournalEntry)
            .Where(t => t.BankAccountId == bankAccountId)
            .AsQueryable();

        if (from.HasValue) q = q.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TransactionDate <= to.Value);
        if (reconciled.HasValue) q = q.Where(t => t.IsReconciled == reconciled.Value);

        var list = await q.OrderByDescending(t => t.TransactionDate).ToListAsync(ct);
        return list.Select(t => new BankTransactionDto(
            t.Id, t.TransactionDate, t.Type, t.Amount, t.Description, t.Reference,
            t.IsReconciled, t.JournalEntry?.EntryNumber)).ToList();
    }

    public async Task<BankTransaction> AddTransactionAsync(BankTransaction tx, CancellationToken ct = default)
    {
        _context.BankTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<(bool Ok, string? Error)> ReconcileAsync(Guid transactionId, Guid journalEntryId, CancellationToken ct = default)
    {
        var tx = await _context.BankTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (tx is null) return (false, "Transacción no encontrada.");
        if (tx.IsReconciled) return (false, "La transacción ya está conciliada.");

        var entry = await _context.JournalEntries.FirstOrDefaultAsync(j => j.Id == journalEntryId, ct);
        if (entry is null) return (false, "Asiento contable no encontrado.");
        if (entry.Status != JournalEntryStatus.Posted) return (false, "El asiento debe estar contabilizado para conciliar.");

        tx.IsReconciled = true;
        tx.ReconciledAt = DateTime.UtcNow;
        tx.JournalEntryId = journalEntryId;
        await _context.SaveChangesAsync(ct);
        return (true, null);
    }
}
