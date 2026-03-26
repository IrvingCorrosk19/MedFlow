using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Analytics;

public sealed class TenantHealthService : ITenantHealthService
{
    private readonly ApplicationDbContext _db;

    public TenantHealthService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TenantHealthVm> GetHealthScoreAsync(Guid tenantId, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-30);
        var to = DateTime.UtcNow.Date;
        var snapshots = await _db.TenantDailySnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .ToListAsync(ct);

        if (snapshots.Count == 0)
            return new TenantHealthVm(0, "Sin datos", "No hay snapshots recientes para calcular el score.", [], ["Ejecutar agregación de snapshots"]);

        var totalApt = snapshots.Sum(s => s.AppointmentsCompleted);
        var totalCancelled = snapshots.Sum(s => s.AppointmentsCancelled);
        var totalNoShow = snapshots.Sum(s => s.AppointmentsNoShow);
        var totalScheduled = snapshots.Sum(s => s.AppointmentsTotal);
        var totalWf = snapshots.Sum(s => s.WorkflowExecutionsTotal);
        var totalWfSuccess = snapshots.Sum(s => s.WorkflowExecutionsSuccess);
        var totalRev = snapshots.Sum(s => s.RevenueCollected);

        var factors = new List<string>();
        var recommendations = new List<string>();
        var score = 70m;

        if (totalScheduled > 0)
        {
            var completionRate = totalApt * 100m / totalScheduled;
            var cancelRate = totalCancelled * 100m / totalScheduled;
            var noShowRate = totalNoShow * 100m / totalScheduled;
            factors.Add($"Tasa completitud: {completionRate:F0}%");
            factors.Add($"Tasa cancelación: {cancelRate:F0}%");
            factors.Add($"Tasa no-show: {noShowRate:F0}%");
            if (completionRate >= 80) score += 5; else if (completionRate < 60) { score -= 10; recommendations.Add("Mejorar confirmación de citas"); }
            if (noShowRate > 15) { score -= 5; recommendations.Add("Reforzar recordatorios para reducir no-show"); }
        }

        if (totalWf > 0)
        {
            var wfRate = totalWfSuccess * 100m / totalWf;
            factors.Add($"Éxito workflows: {wfRate:F0}%");
            if (wfRate >= 90) score += 5; else if (wfRate < 70) { score -= 5; recommendations.Add("Revisar automatizaciones fallidas"); }
        }

        if (totalRev > 0)
            factors.Add($"Ingresos (30d): {totalRev:N0}");

        score = Math.Clamp(score, 0, 100);
        var classification = score >= 85 ? "Excelente" : score >= 70 ? "Bueno" : score >= 50 ? "Regular" : "Requiere atención";
        var summary = $"Score operativo: {score:F0}/100 ({classification})";
        return new TenantHealthVm(score, classification, summary, factors, recommendations);
    }
}
