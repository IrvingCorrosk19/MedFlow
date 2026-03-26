namespace MedFlow.Application.Interfaces;

public interface IAnalyticsAggregationService
{
    Task AggregateDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task AggregateTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default);
    Task AggregateTenantForDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AggregateTodayAsync(CancellationToken cancellationToken = default);
    Task AggregateAllTenantsForDateAsync(DateTime date, CancellationToken cancellationToken = default);
}
