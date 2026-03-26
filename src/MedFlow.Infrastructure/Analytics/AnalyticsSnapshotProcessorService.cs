using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class AnalyticsSnapshotProcessorService
{
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public AnalyticsSnapshotProcessorService(IAnalyticsAggregationService aggregation, ApplicationDbContext db, ITenantContext tenant)
    {
        _aggregation = aggregation;
        _db = db;
        _tenant = tenant;
    }

    public async Task ProcessDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await LogJobStartAsync("DailySnapshot", null, date, cancellationToken);
        try
        {
            await _aggregation.AggregateAllTenantsForDateAsync(date, cancellationToken);
            await LogJobCompleteAsync("DailySnapshot", null, date, cancellationToken);
        }
        catch (Exception ex)
        {
            await LogJobFailAsync("DailySnapshot", null, date, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task ProcessTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        await LogJobStartAsync("TenantSnapshot", tenantId, date, cancellationToken);
        try
        {
            await _aggregation.AggregateTenantForDateAsync(tenantId, date, cancellationToken);
            await LogJobCompleteAsync("TenantSnapshot", tenantId, date, cancellationToken);
        }
        catch (Exception ex)
        {
            await LogJobFailAsync("TenantSnapshot", tenantId, date, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task ProcessTenantForDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        await LogJobStartAsync("TenantSnapshotRange", tenantId, from, cancellationToken);
        try
        {
            await _aggregation.AggregateTenantForDateRangeAsync(tenantId, from, to, cancellationToken);
            await LogJobCompleteAsync("TenantSnapshotRange", tenantId, from, cancellationToken);
        }
        catch (Exception ex)
        {
            await LogJobFailAsync("TenantSnapshotRange", tenantId, from, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task ProcessTodayAsync(CancellationToken cancellationToken = default)
    {
        await LogJobStartAsync("TodaySnapshot", null, DateTime.UtcNow.Date, cancellationToken);
        try
        {
            await _aggregation.AggregateTodayAsync(cancellationToken);
            await LogJobCompleteAsync("TodaySnapshot", null, DateTime.UtcNow.Date, cancellationToken);
        }
        catch (Exception ex)
        {
            await LogJobFailAsync("TodaySnapshot", null, DateTime.UtcNow.Date, ex.Message, cancellationToken);
            throw;
        }
    }

    private async Task LogJobStartAsync(string jobType, Guid? tenantId, DateTime? snapshotDate, CancellationToken ct)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            _db.AnalyticsJobLogs.Add(new AnalyticsJobLog
            {
                JobType = jobType,
                TenantId = tenantId,
                SnapshotDate = snapshotDate,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }

    private async Task LogJobCompleteAsync(string jobType, Guid? tenantId, DateTime? snapshotDate, CancellationToken ct)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var log = await _db.AnalyticsJobLogs
                .Where(l => l.JobType == jobType && l.Status == "Running" && l.TenantId == tenantId && l.SnapshotDate == snapshotDate)
                .OrderByDescending(l => l.StartedAt)
                .FirstOrDefaultAsync(ct);
            if (log != null)
            {
                log.Status = "Completed";
                log.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }

    private async Task LogJobFailAsync(string jobType, Guid? tenantId, DateTime? snapshotDate, string error, CancellationToken ct)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var log = await _db.AnalyticsJobLogs
                .Where(l => l.JobType == jobType && l.Status == "Running" && l.TenantId == tenantId && l.SnapshotDate == snapshotDate)
                .OrderByDescending(l => l.StartedAt)
                .FirstOrDefaultAsync(ct);
            if (log != null)
            {
                log.Status = "Failed";
                log.CompletedAt = DateTime.UtcNow;
                log.ErrorMessage = error.Length > 2000 ? error[..2000] : error;
                await _db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }
}
