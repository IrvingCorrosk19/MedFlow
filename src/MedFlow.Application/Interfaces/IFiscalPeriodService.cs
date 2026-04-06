using MedFlow.Application.Accounting;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IFiscalPeriodService
{
    Task<IReadOnlyList<FiscalPeriodDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<FiscalPeriod?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FiscalPeriod> GetOrCreateAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> CloseAsync(Guid id, string userId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ReopenAsync(Guid id, string userId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> CloseYearAsync(Guid tenantId, int year, string userId, CancellationToken ct = default);
}
