using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class AISettingsService : IAISettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _audit;

    public AISettingsService(IApplicationDbContext context, IAuditLogService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<bool> IsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.Enabled, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsNoShowEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.NoShowEnabled, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsPaymentRiskEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.PaymentRiskEnabled, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.RecommendationsEnabled, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsCopilotEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.CopilotEnabled, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<int> GetMaxDailyInsightsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.MaxDailyInsights, "100", cancellationToken);
        return int.TryParse(v, out var n) && n > 0 ? n : 100;
    }

    public async Task<decimal> GetConfidenceThresholdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.ConfidenceThreshold, "0.6", cancellationToken);
        return decimal.TryParse(v, out var d) && d >= 0 && d <= 1 ? d : 0.6m;
    }

    public async Task<bool> GetShowExplanationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.ShowExplanations, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> GetAllowOperationalSuggestionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var v = await GetSettingAsync(tenantId, AISettingsKeys.AllowOperationalSuggestions, "true", cancellationToken);
        return v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<AITenantSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && s.SettingKey.StartsWith("ai."))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "", cancellationToken);

        string Get(string key, string def) => settings.TryGetValue(key, out var v) ? v : def;
        bool Bool(string key, bool def) => Get(key, def ? "true" : "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        int Int(string key, int def) => int.TryParse(Get(key, def.ToString()), out var n) ? n : def;
        decimal Dec(string key, decimal def) => decimal.TryParse(Get(key, def.ToString()), out var d) ? d : def;

        return new AITenantSettingsDto(
            Bool(AISettingsKeys.Enabled, true),
            Bool(AISettingsKeys.NoShowEnabled, true),
            Bool(AISettingsKeys.PaymentRiskEnabled, true),
            Bool(AISettingsKeys.RecommendationsEnabled, true),
            Bool(AISettingsKeys.CopilotEnabled, true),
            Int(AISettingsKeys.MaxDailyInsights, 100),
            Dec(AISettingsKeys.ConfidenceThreshold, 0.6m),
            Bool(AISettingsKeys.ShowExplanations, true),
            Bool(AISettingsKeys.AllowAutoRecommendations, false),
            Bool(AISettingsKeys.AllowOperationalSuggestions, true));
    }

    public async Task UpdateSettingsAsync(Guid tenantId, AITenantSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var keys = new[] {
            (AISettingsKeys.Enabled, settings.Enabled.ToString().ToLowerInvariant()),
            (AISettingsKeys.NoShowEnabled, settings.NoShowEnabled.ToString().ToLowerInvariant()),
            (AISettingsKeys.PaymentRiskEnabled, settings.PaymentRiskEnabled.ToString().ToLowerInvariant()),
            (AISettingsKeys.RecommendationsEnabled, settings.RecommendationsEnabled.ToString().ToLowerInvariant()),
            (AISettingsKeys.CopilotEnabled, settings.CopilotEnabled.ToString().ToLowerInvariant()),
            (AISettingsKeys.MaxDailyInsights, settings.MaxDailyInsights.ToString()),
            (AISettingsKeys.ConfidenceThreshold, settings.ConfidenceThreshold.ToString("F2")),
            (AISettingsKeys.ShowExplanations, settings.ShowExplanations.ToString().ToLowerInvariant()),
            (AISettingsKeys.AllowAutoRecommendations, settings.AllowAutoRecommendations.ToString().ToLowerInvariant()),
            (AISettingsKeys.AllowOperationalSuggestions, settings.AllowOperationalSuggestions.ToString().ToLowerInvariant())
        };

        var keyList = keys.Select(k => k.Item1).ToList();
        var existing = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && keyList.Contains(s.SettingKey))
            .ToDictionaryAsync(s => s.SettingKey, cancellationToken);

        foreach (var (key, value) in keys)
        {
            if (existing.TryGetValue(key, out var s))
            {
                s.SettingValue = value;
                s.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TenantSettings.Add(new TenantSettings
                {
                    TenantId = tenantId,
                    SettingKey = key,
                    SettingValue = value,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "AI", "AISettings", tenantId.ToString(),
            "Configuración de IA actualizada", null, System.Text.Json.JsonSerializer.Serialize(new { settings.Enabled, settings.NoShowEnabled, settings.PaymentRiskEnabled })), cancellationToken);
    }

    private async Task<string> GetSettingAsync(Guid tenantId, string key, string defaultValue, CancellationToken cancellationToken)
    {
        var s = await _context.TenantSettings
            .Where(x => x.TenantId == tenantId && x.SettingKey == key)
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrEmpty(s) ? defaultValue : s;
    }
}
