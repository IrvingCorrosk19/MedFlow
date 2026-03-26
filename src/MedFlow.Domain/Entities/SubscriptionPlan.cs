using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

/// <summary>Catálogo global de planes SaaS (no multi-tenant).</summary>
public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal MonthlyPrice { get; set; }
    public decimal? AnnualPrice { get; set; }
    public string Currency { get; set; } = "USD";

    public int MaxUsers { get; set; }
    public int MaxDoctors { get; set; }
    public int MaxPatients { get; set; }
    public int MaxAppointmentsPerMonth { get; set; }
    public int? MaxBranches { get; set; }

    public bool IncludesBillingModule { get; set; }
    public bool IncludesAutomationModule { get; set; }
    public bool IncludesReportsModule { get; set; }
    public bool IncludesPatientPortal { get; set; }
    public bool IncludesMultiBranch { get; set; }
    public bool IncludesAdvancedAnalytics { get; set; }

    public int TrialDays { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Stripe Price ID para facturación mensual.</summary>
    public string? StripePriceIdMonthly { get; set; }
    /// <summary>Stripe Price ID para facturación anual.</summary>
    public string? StripePriceIdAnnual { get; set; }
    /// <summary>Stripe Product ID.</summary>
    public string? StripeProductId { get; set; }

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = [];
}
