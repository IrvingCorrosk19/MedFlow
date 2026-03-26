namespace MedFlow.Application.Saas;

public sealed class TenantUsageDto
{
    public int Users { get; init; }
    public int Doctors { get; init; }
    public int Patients { get; init; }
    public int AppointmentsThisMonth { get; init; }

    public int MaxUsers { get; init; }
    public int MaxDoctors { get; init; }
    public int MaxPatients { get; init; }
    public int MaxAppointmentsPerMonth { get; init; }

    public bool IncludesBillingModule { get; init; }
    public bool IncludesAutomationModule { get; init; }
    public bool IncludesReportsModule { get; init; }
    public bool IncludesPatientPortal { get; init; }
    public bool IncludesMultiBranch { get; init; }
    public bool IncludesAdvancedAnalytics { get; init; }

    public double UsersUsagePercent => Percent(Users, MaxUsers);
    public double DoctorsUsagePercent => Percent(Doctors, MaxDoctors);
    public double PatientsUsagePercent => Percent(Patients, MaxPatients);
    public double AppointmentsUsagePercent => Percent(AppointmentsThisMonth, MaxAppointmentsPerMonth);

    private static double Percent(int used, int max)
    {
        if (max <= 0) return 0;
        return Math.Round(100.0 * used / max, 1);
    }
}
