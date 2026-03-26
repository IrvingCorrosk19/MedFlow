namespace MedFlow.Application.Interfaces.AI;

public interface IAISettingsService
{
    Task<bool> IsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsNoShowEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsPaymentRiskEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsCopilotEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> GetMaxDailyInsightsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<decimal> GetConfidenceThresholdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> GetShowExplanationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> GetAllowOperationalSuggestionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<AITenantSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(Guid tenantId, AITenantSettingsDto settings, CancellationToken cancellationToken = default);
}

public record AITenantSettingsDto(
    bool Enabled = true,
    bool NoShowEnabled = true,
    bool PaymentRiskEnabled = true,
    bool RecommendationsEnabled = true,
    bool CopilotEnabled = true,
    int MaxDailyInsights = 100,
    decimal ConfidenceThreshold = 0.6m,
    bool ShowExplanations = true,
    bool AllowAutoRecommendations = false,
    bool AllowOperationalSuggestions = true);
