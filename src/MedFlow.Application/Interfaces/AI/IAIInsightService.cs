using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces.AI;

public interface IAIInsightService
{
    Task<AIInsight> CreateAsync(CreateAIInsightCommand command, CancellationToken cancellationToken = default);
    Task<AIInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIInsight>> ListAsync(AIInsightFilter filter, CancellationToken cancellationToken = default);
    Task<AIInsightDashboardMetrics> GetDashboardMetricsAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task DismissAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}

public record CreateAIInsightCommand(
    Guid TenantId,
    AIInsightType InsightType,
    string EntityType,
    string? EntityId,
    string Title,
    string Summary,
    AISeverity Severity,
    decimal? Score = null,
    decimal? Confidence = null,
    string? Recommendation = null,
    string? EvidenceJson = null,
    string Source = "RuleEngine");

public record AIInsightFilter(
    Guid? TenantId = null,
    AIInsightType? InsightType = null,
    AIInsightStatus? Status = null,
    AISeverity? Severity = null,
    DateTime? From = null,
    DateTime? To = null,
    decimal? MinScore = null,
    decimal? MinConfidence = null,
    string? EntityType = null,
    string? EntityId = null,
    int Page = 1,
    int PageSize = 50);

public record AIInsightDashboardMetrics(
    int TotalGenerated,
    int CriticalCount,
    int WarningCount,
    int NewCount,
    int AcknowledgedCount,
    IReadOnlyList<AIInsightTypeCount> ByType,
    IReadOnlyList<AIInsight> RecentCritical);

public record AIInsightTypeCount(AIInsightType Type, string Label, int Count);
