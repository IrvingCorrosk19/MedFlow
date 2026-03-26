using MedFlow.Domain.Enums;

namespace MedFlow.Application.Saas;

public sealed class SaasTenantListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public TenantCommercialStatus CommercialStatus { get; init; }
    public bool IsSuspended { get; init; }
    public string? PlanName { get; init; }
    public SubscriptionStatus? SubscriptionStatus { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public sealed class SaasTenantDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Email { get; init; }
    public TenantCommercialStatus CommercialStatus { get; init; }
    public bool IsSuspended { get; init; }
    public string? SuspensionReason { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? SuspendedAt { get; init; }

    public Guid? TenantSubscriptionId { get; init; }
    public Guid? SubscriptionPlanId { get; init; }
    public string? PlanName { get; init; }
    public string? PlanCode { get; init; }
    public SubscriptionStatus? SubscriptionStatus { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? NextBillingDate { get; init; }

    public TenantUsageDto Usage { get; init; } = null!;
    public IReadOnlyList<SaasSubscriptionHistoryItemDto> History { get; init; } = [];
}

public sealed class SaasSubscriptionHistoryItemDto
{
    public Guid Id { get; init; }
    public string? PreviousPlanName { get; init; }
    public string NewPlanName { get; init; } = "";
    public SubscriptionStatus? PreviousStatus { get; init; }
    public SubscriptionStatus NewStatus { get; init; }
    public string ChangeReason { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}

public sealed class SaasTenantCreateDto
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Email { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public bool StartWithTrial { get; init; }
}

public sealed class SubscriptionPlanListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public decimal MonthlyPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public sealed class SubscriptionPlanEditDto
{
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal? AnnualPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public int MaxUsers { get; init; }
    public int MaxDoctors { get; init; }
    public int MaxPatients { get; init; }
    public int MaxAppointmentsPerMonth { get; init; }
    public int? MaxBranches { get; init; }
    public bool IncludesBillingModule { get; init; }
    public bool IncludesAutomationModule { get; init; }
    public bool IncludesReportsModule { get; init; }
    public bool IncludesPatientPortal { get; init; }
    public bool IncludesMultiBranch { get; init; }
    public bool IncludesAdvancedAnalytics { get; init; }
    public int TrialDays { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? StripePriceIdMonthly { get; init; }
    public string? StripePriceIdAnnual { get; init; }
    public string? StripeProductId { get; init; }
}

public sealed class SaasSubscriptionListItemDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = "";
    public string TenantCode { get; init; } = "";
    public string PlanName { get; init; } = "";
    public SubscriptionStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? NextBillingDate { get; init; }
}

public sealed class SaasSubscriptionDetailsDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = "";
    public string TenantCode { get; init; } = "";
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; } = "";
    public string PlanCode { get; init; } = "";
    public SubscriptionStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? TrialStartDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? NextBillingDate { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? SuspendedAt { get; init; }
    public string? Notes { get; init; }
    public string? ExternalSubscriptionId { get; init; }
    public string? ExternalPlanId { get; init; }
    public TenantUsageDto Usage { get; init; } = null!;
}
