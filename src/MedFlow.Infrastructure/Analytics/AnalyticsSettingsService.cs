using MedFlow.Application.Interfaces;
using MedFlow.Domain;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class AnalyticsSettingsService : IAnalyticsSettingsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsSettingsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var settings = await _db.TenantSettings
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.SettingKey.StartsWith("analytics."))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "", ct);
        string Get(string key, string def) => settings.TryGetValue(key, out var v) ? v : def;
        return new AnalyticsSettingsDto(
            Bool(Get(AnalyticsSettingsKeys.Enabled, "true")),
            Bool(Get(AnalyticsSettingsKeys.BenchmarkingEnabled, "true")),
            Bool(Get(AnalyticsSettingsKeys.AISummaryEnabled, "true")),
            Bool(Get(AnalyticsSettingsKeys.PortalUsageTrackingEnabled, "false")));
    }

    public async Task UpdateSettingsAsync(Guid tenantId, AnalyticsSettingsDto settings, CancellationToken ct = default)
    {
        var pairs = new[] {
            (AnalyticsSettingsKeys.Enabled, settings.Enabled.ToString().ToLowerInvariant()),
            (AnalyticsSettingsKeys.BenchmarkingEnabled, settings.BenchmarkingEnabled.ToString().ToLowerInvariant()),
            (AnalyticsSettingsKeys.AISummaryEnabled, settings.AISummaryEnabled.ToString().ToLowerInvariant()),
            (AnalyticsSettingsKeys.PortalUsageTrackingEnabled, settings.PortalUsageTrackingEnabled.ToString().ToLowerInvariant()),
        };
        foreach (var (key, value) in pairs)
        {
            var existing = await _db.TenantSettings.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key, ct);
            if (existing != null)
            {
                existing.SettingValue = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.TenantSettings.Add(new Domain.Entities.TenantSettings
                {
                    TenantId = tenantId,
                    SettingKey = key,
                    SettingValue = value
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private static bool Bool(string v) => v.Equals("true", StringComparison.OrdinalIgnoreCase);
}
