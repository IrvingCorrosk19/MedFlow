using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class EventLogQueryService : IEventLogQueryService
{
    private readonly IApplicationDbContext _context;

    public EventLogQueryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EventLog>> GetRecentAsync(int take = 200, OutboxEventStatus? status = null, CancellationToken cancellationToken = default)
    {
        var q = _context.EventLogs.AsNoTracking().AsQueryable();
        if (status.HasValue)
            q = q.Where(e => e.Status == status.Value);
        return await q
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
