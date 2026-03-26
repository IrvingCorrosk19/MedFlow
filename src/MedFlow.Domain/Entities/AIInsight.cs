using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class AIInsight : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public AIInsightType InsightType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public AISeverity Severity { get; set; } = AISeverity.Info;
    public decimal? Score { get; set; }
    public decimal? Confidence { get; set; }
    public string? Recommendation { get; set; }
    public string? EvidenceJson { get; set; }
    public AIInsightStatus Status { get; set; } = AIInsightStatus.New;
    public string Source { get; set; } = "RuleEngine";
    public DateTime GeneratedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByUserId { get; set; }
}
