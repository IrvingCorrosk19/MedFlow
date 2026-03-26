using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class AnalyticsRebuildService : IAnalyticsRebuildService
{
    private readonly AnalyticsSnapshotProcessorService _processor;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public AnalyticsRebuildService(AnalyticsSnapshotProcessorService processor, ApplicationDbContext db, ITenantContext tenant)
    {
        _processor = processor;
        _db = db;
        _tenant = tenant;
    }

    public async Task<RebuildResult> RebuildTenantAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var messages = new List<string>();
        var errors = 0;
        var processed = 0;
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            try
            {
                await _processor.ProcessTenantForDateAsync(tenantId, d, ct);
                processed++;
                messages.Add($"OK {d:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                errors++;
                messages.Add($"Error {d:yyyy-MM-dd}: {ex.Message}");
            }
        }
        return new RebuildResult(processed, errors, messages);
    }

    public async Task<RebuildResult> RebuildTenantForDateAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        try
        {
            await _processor.ProcessTenantForDateAsync(tenantId, date.Date, ct);
            return new RebuildResult(1, 0, [$"OK {date:yyyy-MM-dd}"]);
        }
        catch (Exception ex)
        {
            return new RebuildResult(0, 1, [$"Error: {ex.Message}"]);
        }
    }

    public async Task<RebuildResult> RebuildAllTenantsForDateAsync(DateTime date, CancellationToken ct = default)
    {
        try
        {
            await _processor.ProcessDateAsync(date.Date, ct);
            var tenantCount = await _db.TenantDailySnapshots
                .Where(s => s.SnapshotDate == date.Date)
                .Select(s => s.TenantId)
                .Distinct()
                .CountAsync(ct);
            return new RebuildResult(tenantCount, 0, [$"OK {date:yyyy-MM-dd} - {tenantCount} tenants"]);
        }
        catch (Exception ex)
        {
            return new RebuildResult(0, 1, [$"Error: {ex.Message}"]);
        }
    }

    public async Task<IReadOnlyList<AnalyticsJobLogVm>> GetRecentJobLogsAsync(Guid? tenantId = null, int limit = 50, CancellationToken ct = default)
    {
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var rows = await _db.AnalyticsJobLogs
                .AsNoTracking()
                .Where(l => !tenantId.HasValue || l.TenantId == tenantId)
                .OrderByDescending(l => l.StartedAt)
                .Take(limit)
                .ToListAsync(ct);
            return rows.Select(l => new AnalyticsJobLogVm(l.Id, l.JobType, l.TenantId, l.SnapshotDate, l.Status, l.StartedAt, l.CompletedAt, l.ErrorMessage, l.CreatedAt)).ToList();
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(false);
        }
    }
}
