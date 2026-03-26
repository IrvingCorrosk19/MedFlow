namespace MedFlow.Application.Reporting;

public record AdvancedAnalyticsFilter(
    Guid? TenantId = null,
    DateTime? From = null,
    DateTime? To = null,
    int MaxDays = 90);

public record TrendPointVm(DateTime Date, decimal Value, int? Count = null);

public record TenantBenchmarkVm(
    Guid TenantId,
    string TenantName,
    decimal AppointmentsPerDayAvg,
    decimal RevenuePerDayAvg,
    decimal CompletionRate,
    decimal CancellationRate,
    int PercentileAppointments,
    int PercentileRevenue,
    int RankByAppointments,
    int RankByRevenue);

public record ExecutiveAdvancedDashboardVm(
    IReadOnlyList<TrendPointVm> AppointmentsTrend,
    IReadOnlyList<TrendPointVm> RevenueTrend,
    IReadOnlyList<TrendPointVm> CompletionRateTrend,
    IReadOnlyList<TrendPointVm> NewPatientsTrend,
    TenantBenchmarkVm? Benchmark,
    IReadOnlyList<SnapshotSummaryVm> Last7DaysSummary,
    IReadOnlyList<SnapshotSummaryVm> MonthOverMonthComparison,
    AdvancedKpisVm Kpis);

public record SnapshotSummaryVm(
    DateTime Date,
    int AppointmentsCompleted,
    int AppointmentsCancelled,
    decimal Revenue,
    int NewPatients,
    int AIInsightsGenerated,
    int WorkflowExecutions);

public record AdvancedKpisVm(
    decimal AvgAppointmentsPerDay,
    decimal AvgRevenuePerDay,
    decimal CompletionRateAvg,
    decimal CancellationRateAvg,
    decimal RevenueGrowthPercent,
    decimal AppointmentsGrowthPercent,
    int TotalInsightsPeriod,
    decimal WorkflowSuccessRate);
