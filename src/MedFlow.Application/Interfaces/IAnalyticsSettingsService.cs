namespace MedFlow.Application.Interfaces;

public interface IAnalyticsSettingsService
{
    Task<AnalyticsSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(Guid tenantId, AnalyticsSettingsDto settings, CancellationToken cancellationToken = default);
}

public record AnalyticsSettingsDto(
    bool Enabled = true,
    bool BenchmarkingEnabled = true,
    bool AISummaryEnabled = true,
    bool PortalUsageTrackingEnabled = false);
