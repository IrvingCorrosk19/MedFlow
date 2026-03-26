using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class AIInsightProcessorService : IAIInsightProcessorService
{
    private readonly IApplicationDbContext _context;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIInsightService _insightService;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IEventLogService _eventLog;
    private readonly IAuditLogService _audit;

    public AIInsightProcessorService(
        IApplicationDbContext context,
        IAISettingsService aiSettings,
        IAIInsightService insightService,
        IRecommendationEngine recommendationEngine,
        IEventLogService eventLog,
        IAuditLogService audit)
    {
        _context = context;
        _aiSettings = aiSettings;
        _insightService = insightService;
        _recommendationEngine = recommendationEngine;
        _eventLog = eventLog;
        _audit = audit;
    }

    public async Task ProcessTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!await _aiSettings.IsEnabledAsync(tenantId, cancellationToken))
            return;

        var maxDaily = await _aiSettings.GetMaxDailyInsightsAsync(tenantId, cancellationToken);
        var threshold = await _aiSettings.GetConfidenceThresholdAsync(tenantId, cancellationToken);
        var today = DateTime.UtcNow.Date;

        var countToday = await _context.AIInsights
            .CountAsync(i => i.TenantId == tenantId && i.GeneratedAt >= today, cancellationToken);
        if (countToday >= maxDaily)
            return;

        var recommendations = await _recommendationEngine.GenerateRecommendationsAsync(tenantId, cancellationToken);
        var toCreate = Math.Min(recommendations.Count, maxDaily - countToday);

        foreach (var rec in recommendations.Take(toCreate))
        {
            if (countToday >= maxDaily) break;

            var insightType = rec.Type switch
            {
                "NoShowRisk" => AIInsightType.NoShowRisk,
                "PaymentRisk" => AIInsightType.PaymentRisk,
                "ReengagementOpportunity" => AIInsightType.ReengagementOpportunity,
                _ => AIInsightType.OperationalAnomaly
            };

            var severity = rec.Severity switch
            {
                "Critical" => AISeverity.Critical,
                "Warning" => AISeverity.Warning,
                _ => AISeverity.Info
            };

            var insight = await _insightService.CreateAsync(new CreateAIInsightCommand(
                tenantId,
                insightType,
                rec.EntityType ?? "Unknown",
                rec.EntityId,
                rec.Title,
                rec.Description,
                severity,
                null,
                0.8m,
                rec.SuggestedAction,
                rec.EvidenceJson,
                "RuleEngine"), cancellationToken);

            var aiEventType = insightType switch
            {
                AIInsightType.NoShowRisk => WorkflowTriggerEvents.NoShowRiskDetected,
                AIInsightType.PaymentRisk => WorkflowTriggerEvents.PaymentRiskDetected,
                AIInsightType.ReengagementOpportunity => WorkflowTriggerEvents.PatientReengagementSuggested,
                _ => WorkflowTriggerEvents.AIInsightGenerated
            };
            await _eventLog.EnqueueForTenantAsync(tenantId, aiEventType, new
            {
                insight.Id,
                insight.InsightType,
                insight.EntityType,
                insight.EntityId,
                insight.Title,
                insight.Severity,
                insight.Score
            }, "AIInsight", insight.Id.ToString(), cancellationToken);

            await _audit.LogForTenantAsync(tenantId, new AuditLogWriteDto(
                "Generate", "AI", "AIInsight", insight.Id.ToString(),
                $"Insight generado: {insight.Title} (tipo: {insight.InsightType})",
                null, System.Text.Json.JsonSerializer.Serialize(new { insight.Score, insight.Severity })), null, null, null, cancellationToken);

            countToday++;
        }
    }
}
