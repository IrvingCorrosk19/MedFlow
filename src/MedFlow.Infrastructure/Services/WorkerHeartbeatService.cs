using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class WorkerHeartbeatService : IWorkerHeartbeatService
{
    private readonly ApplicationDbContext _db;

    public WorkerHeartbeatService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task BeatAsync(string workerName, string status = "Running", string? details = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var existing = await _db.WorkerHeartbeats.FirstOrDefaultAsync(h => h.WorkerName == workerName, ct);
        if (existing != null)
        {
            existing.LastSeenAt = now;
            existing.Status = status;
            existing.Details = details;
            existing.UpdatedAt = now;
        }
        else
        {
            _db.WorkerHeartbeats.Add(new WorkerHeartbeat
            {
                WorkerName = workerName,
                LastSeenAt = now,
                Status = status,
                Details = details,
                UpdatedAt = now
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WorkerHeartbeatVm>> GetRecentAsync(CancellationToken ct = default)
    {
        return await _db.WorkerHeartbeats
            .OrderByDescending(h => h.LastSeenAt)
            .Take(50)
            .Select(h => new WorkerHeartbeatVm(h.WorkerName, h.LastSeenAt, h.Status, h.Details))
            .ToListAsync(ct);
    }
}
