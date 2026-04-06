using MedFlow.Application.Accounting;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface ITaxRateService
{
    Task<IReadOnlyList<TaxRateDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaxRate?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task<TaxRate> CreateAsync(TaxRate rate, CancellationToken ct = default);
    Task UpdateAsync(TaxRate rate, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
