namespace MedFlow.Application.Saas;

public sealed class PlanLimitsDto
{
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
}
