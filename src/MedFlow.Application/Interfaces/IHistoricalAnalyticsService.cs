using MedFlow.Application.Reporting;

namespace MedFlow.Application.Interfaces;

public interface IHistoricalAnalyticsService
{
    Task<IReadOnlyList<TrendPointVm>> GetAppointmentsByDayAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetRevenueByDayAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetCancellationsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetNoShowTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetNewPatientsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetWorkflowSuccessTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendPointVm>> GetAIInsightsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
