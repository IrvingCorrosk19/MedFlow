using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class TenantSubscription : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }

    public DateTime? NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? SuspendedAt { get; set; }

    public string? Notes { get; set; }

    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalPlanId { get; set; }
    public string? ExternalPriceId { get; set; }
    public string? ExternalProductId { get; set; }
    public BillingProvider BillingProvider { get; set; } = BillingProvider.None;
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; } = true;
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? LastBillingSyncAt { get; set; }
}
