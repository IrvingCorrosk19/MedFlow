using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class TenantSubscriptionHistory : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid? PreviousPlanId { get; set; }
    public SubscriptionPlan? PreviousPlan { get; set; }

    public Guid NewPlanId { get; set; }
    public SubscriptionPlan NewPlan { get; set; } = null!;

    public SubscriptionStatus? PreviousStatus { get; set; }
    public SubscriptionStatus NewStatus { get; set; }

    public string ChangeReason { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
}
