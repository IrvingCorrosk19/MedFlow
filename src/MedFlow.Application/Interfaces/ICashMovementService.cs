using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface ICashMovementService
{
    Task<IReadOnlyList<CashMovement>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<(decimal Income, decimal Expense, decimal Adjustment)> GetDayTotalsAsync(DateTime dayLocal, CancellationToken cancellationToken = default);
    Task<(decimal Income, decimal Expense, decimal Adjustment)> GetRangeTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<(MedFlow.Domain.Entities.CashMovement? Created, string? Error)> CreateAsync(MedFlow.Domain.Entities.CashMovement movement, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
