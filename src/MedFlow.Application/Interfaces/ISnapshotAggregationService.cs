namespace MedFlow.Application.Interfaces;

public interface ISnapshotAggregationService
{
    Task AggregateDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task AggregateTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default);
}
