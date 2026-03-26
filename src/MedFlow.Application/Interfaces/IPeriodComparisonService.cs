namespace MedFlow.Application.Interfaces;

public interface IPeriodComparisonService
{
    Task<PeriodComparisonVm> CompareAppointmentsAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken cancellationToken = default);
    Task<PeriodComparisonVm> CompareRevenueAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken cancellationToken = default);
    Task<PeriodComparisonVm> CompareNewPatientsAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken cancellationToken = default);
}

public record PeriodComparisonRequest(
    DateTime? CurrentFrom = null,
    DateTime? CurrentTo = null,
    PeriodComparisonType Type = PeriodComparisonType.ThisWeekVsLastWeek);

public enum PeriodComparisonType
{
    TodayVsYesterday,
    ThisWeekVsLastWeek,
    ThisMonthVsLastMonth,
    Rolling7VsPrevious7,
    Rolling30VsPrevious30,
    YTD
}

public record PeriodComparisonVm(
    decimal CurrentValue,
    decimal PreviousValue,
    decimal AbsoluteChange,
    decimal PercentChange,
    string Direction,
    string CurrentLabel,
    string PreviousLabel);
