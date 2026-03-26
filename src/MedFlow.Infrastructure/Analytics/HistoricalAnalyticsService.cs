using MedFlow.Application.Interfaces;
using MedFlow.Application.Reporting;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class HistoricalAnalyticsService : IHistoricalAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public HistoricalAnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetAppointmentsByDayAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AppointmentsCompleted })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.AppointmentsCompleted, r.AppointmentsCompleted)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetRevenueByDayAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.RevenueCollected })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.RevenueCollected, null)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetCancellationsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AppointmentsCancelled })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.AppointmentsCancelled, r.AppointmentsCancelled)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetNoShowTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AppointmentsNoShow })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.AppointmentsNoShow, r.AppointmentsNoShow)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetNewPatientsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.PatientsNewInPeriod })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.PatientsNewInPeriod, r.PatientsNewInPeriod)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetWorkflowSuccessTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, Total = s.WorkflowExecutionsTotal, Success = s.WorkflowExecutionsSuccess })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.Total > 0 ? (decimal)r.Success / r.Total * 100 : 0, r.Success)).ToList();
    }

    public async Task<IReadOnlyList<TrendPointVm>> GetAIInsightsTrendAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.AIInsightsGenerated })
            .ToListAsync(ct);
        return rows.Select(r => new TrendPointVm(r.SnapshotDate, r.AIInsightsGenerated, r.AIInsightsGenerated)).ToList();
    }
}
