using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class PeriodComparisonService : IPeriodComparisonService
{
    private readonly ApplicationDbContext _db;

    public PeriodComparisonService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PeriodComparisonVm> CompareAppointmentsAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken ct = default)
    {
        var (curFrom, curTo, prevFrom, prevTo, curLabel, prevLabel) = ResolvePeriods(request);
        var cur = await SumAppointmentsAsync(tenantId, curFrom, curTo, ct);
        var prev = await SumAppointmentsAsync(tenantId, prevFrom, prevTo, ct);
        return BuildComparison(cur, prev, curLabel, prevLabel);
    }

    public async Task<PeriodComparisonVm> CompareRevenueAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken ct = default)
    {
        var (curFrom, curTo, prevFrom, prevTo, curLabel, prevLabel) = ResolvePeriods(request);
        var cur = await SumRevenueAsync(tenantId, curFrom, curTo, ct);
        var prev = await SumRevenueAsync(tenantId, prevFrom, prevTo, ct);
        return BuildComparison(cur, prev, curLabel, prevLabel);
    }

    public async Task<PeriodComparisonVm> CompareNewPatientsAsync(Guid tenantId, PeriodComparisonRequest request, CancellationToken ct = default)
    {
        var (curFrom, curTo, prevFrom, prevTo, curLabel, prevLabel) = ResolvePeriods(request);
        var cur = await SumNewPatientsAsync(tenantId, curFrom, curTo, ct);
        var prev = await SumNewPatientsAsync(tenantId, prevFrom, prevTo, ct);
        return BuildComparison(cur, prev, curLabel, prevLabel);
    }

    private static (DateTime curFrom, DateTime curTo, DateTime prevFrom, DateTime prevTo, string curLabel, string prevLabel) ResolvePeriods(PeriodComparisonRequest request)
    {
        var now = DateTime.UtcNow.Date;
        return request.Type switch
        {
            PeriodComparisonType.TodayVsYesterday => (now, now.AddDays(1), now.AddDays(-1), now, "Hoy", "Ayer"),
            PeriodComparisonType.ThisWeekVsLastWeek => (
                now.AddDays(-(int)now.DayOfWeek + 1), now.AddDays(1),
                now.AddDays(-(int)now.DayOfWeek - 6), now.AddDays(-(int)now.DayOfWeek),
                "Esta semana", "Semana anterior"),
            PeriodComparisonType.ThisMonthVsLastMonth => (
                new DateTime(now.Year, now.Month, 1), now.AddDays(1),
                new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1),
                "Este mes", "Mes anterior"),
            PeriodComparisonType.Rolling7VsPrevious7 => (now.AddDays(-6), now.AddDays(1), now.AddDays(-13), now.AddDays(-6), "Últimos 7 días", "7 días anteriores"),
            PeriodComparisonType.Rolling30VsPrevious30 => (now.AddDays(-29), now.AddDays(1), now.AddDays(-59), now.AddDays(-29), "Últimos 30 días", "30 días anteriores"),
            _ => (now.AddDays(-6), now.AddDays(1), now.AddDays(-13), now.AddDays(-6), "Últimos 7 días", "7 días anteriores")
        };
    }

    private async Task<decimal> SumAppointmentsAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate < to)
            .SumAsync(s => s.AppointmentsCompleted, ct);
    }

    private async Task<decimal> SumRevenueAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate < to)
            .SumAsync(s => s.RevenueCollected, ct);
    }

    private async Task<decimal> SumNewPatientsAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate < to)
            .SumAsync(s => s.PatientsNewInPeriod, ct);
    }

    private static PeriodComparisonVm BuildComparison(decimal cur, decimal prev, string curLabel, string prevLabel)
    {
        var change = cur - prev;
        var pct = prev != 0 ? change / prev * 100 : (cur > 0 ? 100 : 0);
        var dir = pct > 0 ? "up" : pct < 0 ? "down" : "stable";
        return new PeriodComparisonVm(cur, prev, change, pct, dir, curLabel, prevLabel);
    }
}
