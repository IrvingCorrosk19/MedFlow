namespace MedFlow.Application.Reporting;

public record ExecutiveDashboardFilter(int ChartDays = 14);

public record AppointmentKpiVm(
    int TotalToday,
    int PendingToday,
    int ConfirmedToday,
    int CancelledToday,
    int CompletedToday,
    int NoShowToday);

public record PatientKpiVm(
    int TotalRegistered,
    int NewThisMonth,
    int DistinctAttendedToday,
    int InactiveCount);

public record DoctorKpiVm(
    int ActiveCount,
    string? TopDoctorByAppointmentsName,
    int TopDoctorAppointmentCount,
    string? TopSpecialityName,
    int TopSpecialityAppointmentCount);

public record FinanceKpiVm(
    decimal BillingToday,
    decimal BillingMonth,
    decimal PaymentsToday,
    decimal TotalOutstanding,
    int InvoicesPendingOrPartial,
    int InvoicesPaid);

public record NamedCountVm(string Name, int Count);

public record NamedDecimalVm(string Name, decimal Amount);

public record DateCountVm(DateTime Date, int Count);

public record DateDecimalVm(DateTime Date, decimal Amount);

public record MonthCountVm(int Year, int Month, int Count);

public record ActivityItemVm(string IconClass, string Text, string WhenLabel, string? LinkUrl, DateTime? Timestamp = null);

public record ExecutiveAlertVm(string Severity, string Message);

public record ExecutiveDashboardVm(
    AppointmentKpiVm AppointmentKpis,
    PatientKpiVm PatientKpis,
    DoctorKpiVm DoctorKpis,
    FinanceKpiVm FinanceKpis,
    IReadOnlyList<DateCountVm> AppointmentsByDay,
    IReadOnlyList<NamedCountVm> AppointmentsByStatus,
    IReadOnlyList<DateDecimalVm> RevenueByDay,
    IReadOnlyList<MonthCountVm> NewPatientsByMonth,
    IReadOnlyList<NamedCountVm> TopSpecialitiesByAppointments,
    IReadOnlyList<NamedCountVm> TopDoctorsByAppointments,
    IReadOnlyList<NamedDecimalVm> PaymentsByMethod,
    IReadOnlyList<DateCountVm> CancellationsByDay,
    decimal CancellationRatePeriod,
    decimal CompletionRatePeriod,
    IReadOnlyList<ActivityItemVm> RecentActivity,
    IReadOnlyList<ExecutiveAlertVm> ExecutiveAlerts,
    IReadOnlyList<UpcomingAppointmentVm> UpcomingAppointments);

public record UpcomingAppointmentVm(
    Guid Id,
    DateTime ScheduledDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string PatientName,
    string DoctorName,
    string StatusLabel);

public record AppointmentsReportFilter(
    DateTime? From,
    DateTime? To,
    Guid? DoctorId,
    string? Speciality,
    int? Status);

public record AppointmentsReportRowVm(
    Guid Id,
    DateTime Date,
    TimeSpan Start,
    string PatientName,
    string DoctorName,
    string Speciality,
    string StatusLabel,
    string? Reason);

public record AppointmentsReportVm(
    IReadOnlyList<AppointmentsReportRowVm> Rows,
    IReadOnlyList<NamedCountVm> TotalsByStatus,
    int TotalCount);

public record PatientsReportFilter(DateTime? From, DateTime? To, bool IncludeInactive);

public record PatientsReportRowVm(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    bool IsActive,
    int AppointmentCount);

public record PatientsReportVm(
    IReadOnlyList<PatientsReportRowVm> Rows,
    int NewInPeriod,
    IReadOnlyList<NamedCountVm> TopByAppointments);

public record FinancialReportFilter(
    DateTime? From,
    DateTime? To,
    Guid? PatientId,
    int? PaymentMethod);

public record FinancialSummaryVm(
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal Outstanding,
    int InvoiceCount);

public record FinancialReportVm(
    FinancialSummaryVm Summary,
    IReadOnlyList<InvoiceReportRowVm> Invoices,
    IReadOnlyList<PaymentReportRowVm> Payments,
    IReadOnlyList<NamedDecimalVm> TotalsByPaymentMethod);

public record InvoiceReportRowVm(
    Guid Id,
    string InvoiceNumber,
    DateTime IssueDate,
    string PatientName,
    decimal Total,
    decimal Balance,
    string StatusLabel);

public record PaymentReportRowVm(
    Guid Id,
    DateTime PaymentDate,
    string PatientName,
    string InvoiceNumber,
    decimal Amount,
    string MethodLabel);

public record DoctorsReportFilter(DateTime? From, DateTime? To, Guid? DoctorId);

public record DoctorsReportRowVm(
    Guid DoctorId,
    string DoctorName,
    string? Speciality,
    int TotalAppointments,
    int Completed,
    int Cancelled,
    int NoShow);

public record DoctorsReportVm(
    IReadOnlyList<DoctorsReportRowVm> Rows,
    decimal AvgAppointmentsPerDoctor);
