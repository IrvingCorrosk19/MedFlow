using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class AdvancedAnalyticsService : IAdvancedAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public AdvancedAnalyticsService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ExecutiveAdvancedDashboardVm> GetExecutiveAdvancedDashboardAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = filter.TenantId ?? _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var from = filter.From ?? DateTime.UtcNow.Date.AddDays(-filter.MaxDays);
        var to = filter.To ?? DateTime.UtcNow.Date;

        var snapshots = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(cancellationToken);

        var aptTrend = snapshots.Select(s => new TrendPointVm(s.SnapshotDate, s.AppointmentsCompleted, s.AppointmentsCompleted)).ToList();
        var revTrend = snapshots.Select(s => new TrendPointVm(s.SnapshotDate, s.RevenueCollected)).ToList();
        var completionTrend = snapshots
            .Where(s => s.AppointmentsTotal > 0)
            .Select(s => new TrendPointVm(s.SnapshotDate, (decimal)s.AppointmentsCompleted / s.AppointmentsTotal * 100))
            .ToList();
        var newPatientsTrend = snapshots.Select(s => new TrendPointVm(s.SnapshotDate, s.PatientsNewInPeriod, s.PatientsNewInPeriod)).ToList();

        var benchmark = await GetTenantBenchmarkAsync(tenantId, from, to, cancellationToken);

        var last7 = snapshots.OrderByDescending(s => s.SnapshotDate).Take(7).Reverse().ToList();
        var last7Summary = last7.Select(s => new SnapshotSummaryVm(s.SnapshotDate, s.AppointmentsCompleted, s.AppointmentsCancelled, s.RevenueCollected, s.PatientsNewInPeriod, s.AIInsightsGenerated, s.WorkflowExecutionsTotal)).ToList();

        var now = DateTime.UtcNow;
        var thisMonth = snapshots.Where(s => s.SnapshotDate.Month == now.Month && s.SnapshotDate.Year == now.Year).ToList();
        var prevMonth = snapshots.Where(s => { var pm = now.AddMonths(-1); return s.SnapshotDate.Month == pm.Month && s.SnapshotDate.Year == pm.Year; }).ToList();
        var momComparison = new List<SnapshotSummaryVm>();
        if (thisMonth.Count > 0)
            momComparison.Add(new SnapshotSummaryVm(new DateTime(now.Year, now.Month, 1), thisMonth.Sum(x => x.AppointmentsCompleted), thisMonth.Sum(x => x.AppointmentsCancelled), thisMonth.Sum(x => x.RevenueCollected), thisMonth.Sum(x => x.PatientsNewInPeriod), thisMonth.Sum(x => x.AIInsightsGenerated), thisMonth.Sum(x => x.WorkflowExecutionsTotal)));
        if (prevMonth.Count > 0)
        {
            var pm = now.AddMonths(-1);
            momComparison.Add(new SnapshotSummaryVm(new DateTime(pm.Year, pm.Month, 1), prevMonth.Sum(x => x.AppointmentsCompleted), prevMonth.Sum(x => x.AppointmentsCancelled), prevMonth.Sum(x => x.RevenueCollected), prevMonth.Sum(x => x.PatientsNewInPeriod), prevMonth.Sum(x => x.AIInsightsGenerated), prevMonth.Sum(x => x.WorkflowExecutionsTotal)));
        }

        var totalApt = snapshots.Sum(s => s.AppointmentsCompleted);
        var totalCancelled = snapshots.Sum(s => s.AppointmentsCancelled);
        var totalScheduled = snapshots.Sum(s => s.AppointmentsTotal);
        var totalRev = snapshots.Sum(s => s.RevenueCollected);
        var dayCount = snapshots.Select(s => s.SnapshotDate).Distinct().Count();
        var completionRate = totalScheduled > 0 ? (decimal)totalApt / totalScheduled * 100 : 0;
        var cancellationRate = totalScheduled > 0 ? (decimal)totalCancelled / totalScheduled * 100 : 0;

        var firstHalf = snapshots.Take(snapshots.Count / 2).ToList();
        var secondHalf = snapshots.Skip(snapshots.Count / 2).ToList();
        var revFirst = firstHalf.Sum(s => s.RevenueCollected);
        var revSecond = secondHalf.Sum(s => s.RevenueCollected);
        var aptFirst = firstHalf.Sum(s => s.AppointmentsCompleted);
        var aptSecond = secondHalf.Sum(s => s.AppointmentsCompleted);
        var revGrowth = revFirst > 0 ? (revSecond - revFirst) / revFirst * 100 : 0;
        var aptGrowth = aptFirst > 0 ? (aptSecond - aptFirst) / (decimal)aptFirst * 100 : 0;

        var totalWf = snapshots.Sum(s => s.WorkflowExecutionsTotal);
        var wfSuccess = snapshots.Sum(s => s.WorkflowExecutionsSuccess);
        var wfSuccessRate = totalWf > 0 ? (decimal)wfSuccess / totalWf * 100 : 0;

        var kpis = new AdvancedKpisVm(
            dayCount > 0 ? totalApt / (decimal)dayCount : 0,
            dayCount > 0 ? totalRev / dayCount : 0,
            completionRate,
            cancellationRate,
            revGrowth,
            aptGrowth,
            snapshots.Sum(s => s.AIInsightsGenerated),
            wfSuccessRate);

        return new ExecutiveAdvancedDashboardVm(aptTrend, revTrend, completionTrend, newPatientsTrend, benchmark, last7Summary, momComparison, kpis);
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetAppointmentsTrendAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = filter.TenantId ?? _tenant.TenantId;
        var from = filter.From ?? DateTime.UtcNow.Date.AddDays(-filter.MaxDays);
        var to = filter.To ?? DateTime.UtcNow.Date;

        var q = _db.TenantDailySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to);
        if (tenantId.HasValue)
            q = q.Where(s => s.TenantId == tenantId.Value);

        var rows = await q.OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AppointmentsCompleted })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.AppointmentsCompleted, r.AppointmentsCompleted)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetRevenueTrendAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = filter.TenantId ?? _tenant.TenantId;
        var from = filter.From ?? DateTime.UtcNow.Date.AddDays(-filter.MaxDays);
        var to = filter.To ?? DateTime.UtcNow.Date;

        var q = _db.TenantDailySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to);
        if (tenantId.HasValue)
            q = q.Where(s => s.TenantId == tenantId.Value);

        var rows = await q.OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.RevenueCollected })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.RevenueCollected, null)).ToList();
    }

    public async Task<TenantBenchmarkVm?> GetTenantBenchmarkAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        from ??= DateTime.UtcNow.Date.AddDays(-30);
        to ??= DateTime.UtcNow.Date;

        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var tenantSnapshots = await _db.TenantDailySnapshots
                .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
                .ToListAsync(cancellationToken);

            if (tenantSnapshots.Count == 0)
                return null;

            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            var dayCount = tenantSnapshots.Select(s => s.SnapshotDate).Distinct().Count();
            var aptAvg = dayCount > 0 ? tenantSnapshots.Sum(s => s.AppointmentsCompleted) / (decimal)dayCount : 0;
            var revAvg = dayCount > 0 ? tenantSnapshots.Sum(s => s.RevenueCollected) / dayCount : 0;
            var totalApt = tenantSnapshots.Sum(s => s.AppointmentsCompleted);
            var totalCancelled = tenantSnapshots.Sum(s => s.AppointmentsCancelled);
            var totalScheduled = tenantSnapshots.Sum(s => s.AppointmentsTotal);
            var completionRate = totalScheduled > 0 ? (decimal)totalApt / totalScheduled * 100 : 0;
            var cancellationRate = totalScheduled > 0 ? (decimal)totalCancelled / totalScheduled * 100 : 0;

            var allTenantAggregates = await _db.TenantDailySnapshots
                .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to)
                .GroupBy(s => s.TenantId)
                .Select(g => new
                {
                    TenantId = g.Key,
                    DayCount = g.Select(x => x.SnapshotDate).Distinct().Count(),
                    AptSum = g.Sum(x => x.AppointmentsCompleted),
                    RevSum = g.Sum(x => x.RevenueCollected)
                })
                .Where(x => x.DayCount > 0)
                .ToListAsync(cancellationToken);

            var aptAvgs = allTenantAggregates.Select(a => a.AptSum / (decimal)a.DayCount).OrderBy(x => x).ToList();
            var revAvgs = allTenantAggregates.Select(a => a.RevSum / a.DayCount).OrderBy(x => x).ToList();

            var aptPercentile = aptAvgs.Count > 0 ? (int)((aptAvgs.Count(x => x <= aptAvg) / (decimal)aptAvgs.Count) * 100) : 50;
            var revPercentile = revAvgs.Count > 0 ? (int)((revAvgs.Count(x => x <= revAvg) / (decimal)revAvgs.Count) * 100) : 50;

            var aptRank = aptAvgs.Count - aptAvgs.Count(x => x <= aptAvg) + 1;
            var revRank = revAvgs.Count - revAvgs.Count(x => x <= revAvg) + 1;

            return new TenantBenchmarkVm(
                tenantId,
                tenant?.Name ?? "Unknown",
                aptAvg,
                revAvg,
                completionRate,
                cancellationRate,
                aptPercentile,
                revPercentile,
                aptRank,
                revRank);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }

    public async Task<IReadOnlyList<SnapshotSummaryVm>> GetDailySnapshotsAsync(AdvancedAnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = filter.TenantId ?? _tenant.TenantId;
        var from = filter.From ?? DateTime.UtcNow.Date.AddDays(-filter.MaxDays);
        var to = filter.To ?? DateTime.UtcNow.Date;

        var q = _db.TenantDailySnapshots
            .Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to);
        if (tenantId.HasValue)
            q = q.Where(s => s.TenantId == tenantId.Value);

        var rows = await q.OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AppointmentsCompleted, s.AppointmentsCancelled, s.RevenueCollected, s.PatientsNewInPeriod, s.AIInsightsGenerated, s.WorkflowExecutionsTotal })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new SnapshotSummaryVm(r.SnapshotDate, r.AppointmentsCompleted, r.AppointmentsCancelled, r.RevenueCollected, r.PatientsNewInPeriod, r.AIInsightsGenerated, r.WorkflowExecutionsTotal)).ToList();
    }
}
