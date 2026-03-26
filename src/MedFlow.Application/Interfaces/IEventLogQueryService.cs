using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface IEventLogQueryService
{
    Task<IReadOnlyList<EventLog>> GetRecentAsync(int take = 200, OutboxEventStatus? status = null, CancellationToken cancellationToken = default);
}
