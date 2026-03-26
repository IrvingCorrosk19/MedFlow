namespace MedFlow.Application.Interfaces;

public interface IEventLogService
{
    Task EnqueueAsync(string eventType, object payload, string? aggregateType = null, string? aggregateId = null, CancellationToken cancellationToken = default);
    Task EnqueueForTenantAsync(Guid tenantId, string eventType, object payload, string? aggregateType = null, string? aggregateId = null, CancellationToken cancellationToken = default);
}
