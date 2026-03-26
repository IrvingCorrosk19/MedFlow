using MedFlow.Application.Reporting;

namespace MedFlow.Application.Interfaces;

public interface IAdvancedAnalyticsService
{
    Task<ExecutiveAdvancedDashboardVm> GetExecutiveAdvancedDashboardAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetAppointmentsTrendAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetRevenueTrendAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<TenantBenchmarkVm?> GetTenantBenchmarkAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SnapshotSummaryVm>> GetDailySnapshotsAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default);
}
