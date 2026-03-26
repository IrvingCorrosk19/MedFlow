using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class BenchmarkingService : IBenchmarkingService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public BenchmarkingService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<BenchmarkSummaryVm?> GetTenantBenchmarkAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, string? cohortKey = null, CancellationToken ct = default)
    {
        from ??= DateTime.UtcNow.Date.AddDays(-30);
        to ??= DateTime.UtcNow.Date;

        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var tenantSnapshots = await _db.TenantDailySnapshots
                .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
                .ToListAsync(ct);
            if (tenantSnapshots.Count == 0) return null;

            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            var dayCount = Math.Max(1, tenantSnapshots.Select(s => s.SnapshotDate).Distinct().Count());
            var totalApt = tenantSnapshots.Sum(s => s.AppointmentsCompleted);
            var totalCancelled = tenantSnapshots.Sum(s => s.AppointmentsCancelled);
            var totalNoShow = tenantSnapshots.Sum(s => s.AppointmentsNoShow);
            var totalScheduled = tenantSnapshots.Sum(s => s.AppointmentsTotal);
            var totalRev = tenantSnapshots.Sum(s => s.RevenueCollected);

            var completionRate = totalScheduled > 0 ? totalApt * 100m / totalScheduled : 0;
            var cancellationRate = totalScheduled > 0 ? totalCancelled * 100m / totalScheduled : 0;
            var noShowRate = totalScheduled > 0 ? totalNoShow * 100m / totalScheduled : 0;
            var revPerDay = totalRev / dayCount;
            var aptPerDay = totalApt / (decimal)dayCount;

            var planCode = tenantSnapshots.FirstOrDefault()?.PlanCode ?? "";
            var cohortFilter = cohortKey == "plan" && !string.IsNullOrEmpty(planCode)
                ? _db.TenantDailySnapshots.Where(s => s.PlanCode == planCode)
                : _db.TenantDailySnapshots.AsQueryable();

            var allAggregates = await cohortFilter
                .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to)
                .GroupBy(s => s.TenantId)
                .Select(g => new
                {
                    TenantId = g.Key,
                    DayCount = g.Select(x => x.SnapshotDate).Distinct().Count(),
                    AptSum = g.Sum(x => x.AppointmentsCompleted),
                    RevSum = g.Sum(x => x.RevenueCollected),
                    TotalSched = g.Sum(x => x.AppointmentsTotal),
                    TotalCancelled = g.Sum(x => x.AppointmentsCancelled),
                    TotalNoShow = g.Sum(x => x.AppointmentsNoShow),
                    TotalCompleted = g.Sum(x => x.AppointmentsCompleted)
                })
                .Where(x => x.DayCount > 0 && x.TotalSched > 0)
                .ToListAsync(ct);

            var completionRates = allAggregates.Select(a => a.TotalCompleted * 100m / a.TotalSched).OrderBy(x => x).ToList();
            var revPerDays = allAggregates.Select(a => a.RevSum / a.DayCount).OrderBy(x => x).ToList();

            var pctCompletion = completionRates.Count > 0 ? (int)(completionRates.Count(x => x <= completionRate) * 100m / completionRates.Count) : 50;
            var pctRevenue = revPerDays.Count > 0 ? (int)(revPerDays.Count(x => x <= revPerDay) * 100m / revPerDays.Count) : 50;

            var factors = new List<BenchmarkFactorVm>();
            var avgCompletion = completionRates.Count > 0 ? completionRates.Average() : completionRate;
            var avgRev = revPerDays.Count > 0 ? revPerDays.Average() : revPerDay;
            factors.Add(new BenchmarkFactorVm("Tasa completitud", completionRate, avgCompletion, completionRate >= avgCompletion));
            factors.Add(new BenchmarkFactorVm("Ingresos/día", revPerDay, avgRev, revPerDay >= avgRev));

            return new BenchmarkSummaryVm(
                tenantId,
                tenant?.Name ?? "Unknown",
                completionRate,
                cancellationRate,
                noShowRate,
                revPerDay,
                (int)aptPerDay,
                pctCompletion,
                pctRevenue,
                cohortKey,
                factors);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }

    public async Task<IReadOnlyList<CohortBenchmarkVm>> GetCohortAveragesAsync(string cohortKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            IQueryable<TenantDailySnapshot> baseQ = _db.TenantDailySnapshots.Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to);
            var byCohort = cohortKey == "plan"
                ? await baseQ.GroupBy(s => s.PlanCode ?? "default")
                    .Select(g => new { Key = g.Key!, TenantCount = g.Select(x => x.TenantId).Distinct().Count(), TotalSched = g.Sum(x => x.AppointmentsTotal), TotalCompleted = g.Sum(x => x.AppointmentsCompleted), TotalCancelled = g.Sum(x => x.AppointmentsCancelled), TotalRev = g.Sum(x => x.RevenueCollected), DayCount = g.Select(x => x.SnapshotDate).Distinct().Count() })
                    .Where(x => x.TotalSched > 0 && x.DayCount > 0)
                    .ToListAsync(ct)
                : await baseQ.GroupBy(_ => "all")
                    .Select(g => new { Key = g.Key, TenantCount = g.Select(x => x.TenantId).Distinct().Count(), TotalSched = g.Sum(x => x.AppointmentsTotal), TotalCompleted = g.Sum(x => x.AppointmentsCompleted), TotalCancelled = g.Sum(x => x.AppointmentsCancelled), TotalRev = g.Sum(x => x.RevenueCollected), DayCount = g.Select(x => x.SnapshotDate).Distinct().Count() })
                    .Where(x => x.TotalSched > 0 && x.DayCount > 0)
                    .ToListAsync(ct);

            return byCohort.Select(c => new CohortBenchmarkVm(
                c.Key,
                c.Key == "all" ? "Todos" : c.Key,
                c.TenantCount,
                c.TotalSched > 0 ? c.TotalCompleted * 100m / c.TotalSched : 0,
                c.TotalSched > 0 ? c.TotalCancelled * 100m / c.TotalSched : 0,
                c.DayCount > 0 ? c.TotalRev / c.DayCount : 0)).ToList();
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }
}
