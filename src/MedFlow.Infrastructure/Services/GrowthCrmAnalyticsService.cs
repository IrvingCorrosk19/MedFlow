using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class GrowthCrmAnalyticsService : IGrowthCrmAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public GrowthCrmAnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientEngagementSummaryRow>> GetTopPatientsByAppointmentVolumeAsync(
        Guid tenantId,
        int lastDays,
        int take,
        CancellationToken cancellationToken = default)
    {
        lastDays = Math.Clamp(lastDays, 7, 730);
        take = Math.Clamp(take, 1, 50);

        var fromUtc = DateTime.UtcNow.Date.AddDays(-lastDays);

        var counts = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted && a.ScheduledDate >= fromUtc)
            .GroupBy(a => a.PatientId)
            .Select(g => new { PatientId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (counts.Count == 0)
            return Array.Empty<PatientEngagementSummaryRow>();

        var ids = counts.Select(x => x.PatientId).ToList();
        var patients = await _db.Patients
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.PrimerNombre, p.SegundoNombre, p.PrimerApellido, p.SegundoApellido })
            .ToListAsync(cancellationToken);

        string Name(string? a, string? b, string? c, string? d) =>
            string.Join(" ", new[] { a, b, c, d }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        var nameById = patients.ToDictionary(
            x => x.Id,
            x => Name(x.PrimerNombre, x.SegundoNombre, x.PrimerApellido, x.SegundoApellido));

        return counts
            .Select(c => new PatientEngagementSummaryRow(
                c.PatientId,
                nameById.TryGetValue(c.PatientId, out var nm) ? nm : "—",
                c.Cnt))
            .ToList();
    }
}
