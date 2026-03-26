using MedFlow.Application.Saas;

namespace MedFlow.Web.Models.Onboarding;

public sealed class OnboardingPlanCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public int TrialDays { get; init; }
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

    public static OnboardingPlanCardVm FromEdit(Guid id, SubscriptionPlanEditDto d) =>
        new()
        {
            Id = id,
            Name = d.Name,
            Code = d.Code,
            Description = d.Description,
            MonthlyPrice = d.MonthlyPrice,
            Currency = d.Currency,
            TrialDays = d.TrialDays,
            MaxUsers = d.MaxUsers,
            MaxDoctors = d.MaxDoctors,
            MaxPatients = d.MaxPatients,
            MaxAppointmentsPerMonth = d.MaxAppointmentsPerMonth,
            MaxBranches = d.MaxBranches,
            IncludesBillingModule = d.IncludesBillingModule,
            IncludesAutomationModule = d.IncludesAutomationModule,
            IncludesReportsModule = d.IncludesReportsModule,
            IncludesPatientPortal = d.IncludesPatientPortal,
            IncludesMultiBranch = d.IncludesMultiBranch,
            IncludesAdvancedAnalytics = d.IncludesAdvancedAnalytics
        };
}
