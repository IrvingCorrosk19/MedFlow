using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface ICashMovementService
{
    Task<IReadOnlyList<CashMovement>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<(decimal Income, decimal Expense, decimal Adjustment)> GetDayTotalsAsync(DateTime dayLocal, CancellationToken cancellationToken = default);
}
