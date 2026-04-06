using MedFlow.Application.Accounting;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<AccountListItemDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account> CreateAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerateNextCodeAsync(Guid tenantId, string prefix, CancellationToken ct = default);
    /// <summary>Recalcula CurrentBalance de todas las cuentas del tenant.</summary>
    Task RecalculateBalancesAsync(Guid tenantId, CancellationToken ct = default);
}
