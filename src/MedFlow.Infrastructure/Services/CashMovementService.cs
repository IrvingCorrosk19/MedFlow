using MedFlow.Application.Interfaces;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class CashMovementService : ICashMovementService
{
    private readonly IApplicationDbContext _context;

    public CashMovementService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Domain.Entities.CashMovement>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return await _context.CashMovements
            .AsNoTracking()
            .Where(m => m.MovementDate >= fromUtc && m.MovementDate < toUtc)
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(decimal Income, decimal Expense, decimal Adjustment)> GetDayTotalsAsync(DateTime dayUtc, CancellationToken cancellationToken = default)
    {
        var start = dayUtc.Date;
        var end = start.AddDays(1);

        var baseQuery = _context.CashMovements
            .Where(m => m.MovementDate >= start && m.MovementDate < end);

        var income = await baseQuery
            .Where(m => m.MovementType == CashMovementType.Income)
            .SumAsync(m => m.Amount, cancellationToken);

        var expense = await baseQuery
            .Where(m => m.MovementType == CashMovementType.Expense)
            .SumAsync(m => m.Amount, cancellationToken);

        var adj = await baseQuery
            .Where(m => m.MovementType == CashMovementType.Adjustment)
            .SumAsync(m => m.Amount, cancellationToken);

        return (Money.Round(income), Money.Round(expense), Money.Round(adj));
    }
}
