namespace MedFlow.Application.Interfaces;

public interface IAnalyticsRebuildService
{
    Task<RebuildResult> RebuildTenantAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<RebuildResult> RebuildTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default);
    Task<RebuildResult> RebuildAllTenantsForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalyticsJobLogVm>> GetRecentJobLogsAsync(Guid? tenantId = null, int limit = 50, CancellationToken cancellationToken = default);
}

public record RebuildResult(int SnapshotsProcessed, int Errors, IReadOnlyList<string> Messages);

public record AnalyticsJobLogVm(
    Guid Id,
    string JobType,
    Guid? TenantId,
    DateTime? SnapshotDate,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    DateTime CreatedAt);
