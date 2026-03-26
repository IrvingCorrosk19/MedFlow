using MedFlow.Application.Reporting;

namespace MedFlow.Application.Interfaces;

public interface IBenchmarkingService
{
    Task<BenchmarkSummaryVm?> GetTenantBenchmarkAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, string? cohortKey = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CohortBenchmarkVm>> GetCohortAveragesAsync(string cohortKey, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public record BenchmarkSummaryVm(
    Guid TenantId,
    string TenantName,
    decimal CompletionRate,
    decimal CancellationRate,
    decimal NoShowRate,
    decimal RevenuePerDayAvg,
    int AppointmentsPerDayAvg,
    int PercentileCompletion,
    int PercentileRevenue,
    string? CohortLabel,
    IReadOnlyList<BenchmarkFactorVm> Factors);

public record BenchmarkFactorVm(string Label, decimal TenantValue, decimal CohortAvg, bool AboveAverage);

public record CohortBenchmarkVm(string CohortKey, string Label, int TenantCount, decimal AvgCompletionRate, decimal AvgCancellationRate, decimal AvgRevenuePerDay);
