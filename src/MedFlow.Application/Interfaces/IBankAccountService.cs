using MedFlow.Application.Accounting;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<BankAccount?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<BankAccount> CreateAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateAsync(BankAccount account, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BankTransactionDto>> GetTransactionsAsync(
        Guid bankAccountId,
        DateTime? from, DateTime? to,
        bool? reconciled,
        CancellationToken ct = default);

    Task<BankTransaction> AddTransactionAsync(BankTransaction tx, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ReconcileAsync(Guid transactionId, Guid journalEntryId, CancellationToken ct = default);
}
