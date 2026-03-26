using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class OperationalSummaryService : IOperationalSummaryService
{
    private readonly IApplicationDbContext _context;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIInsightService _insightService;

    public OperationalSummaryService(
        IApplicationDbContext context,
        IAISettingsService aiSettings,
        IAIInsightService insightService)
    {
        _context = context;
        _aiSettings = aiSettings;
        _insightService = insightService;
    }

    public async Task<OperationalSummaryDto> GenerateDailySummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        if (!await _aiSettings.IsEnabledAsync(tenantId, cancellationToken))
            return new OperationalSummaryDto(date, "IA desactivada.", 0, 0, []);

        var start = date.Date;
        var end = start.AddDays(1);

        var insights = await _context.AIInsights
            .Where(i => i.TenantId == tenantId && i.GeneratedAt >= start && i.GeneratedAt < end)
            .ToListAsync(cancellationToken);

        var critical = insights.Count(i => i.Severity == AISeverity.Critical);
        var warning = insights.Count(i => i.Severity == AISeverity.Warning);
        var byType = insights.GroupBy(i => i.InsightType).ToDictionary(g => g.Key, g => g.Count());

        var sections = new List<OperationalSummarySection>();
        if (byType.TryGetValue(AIInsightType.NoShowRisk, out var noShow))
            sections.Add(new OperationalSummarySection("Riesgo de inasistencia", $"{noShow} cita(s) con riesgo de no-show.", noShow));
        if (byType.TryGetValue(AIInsightType.PaymentRisk, out var pay))
            sections.Add(new OperationalSummarySection("Riesgo de cobro", $"{pay} paciente(s) con facturas en riesgo.", pay));
        if (byType.TryGetValue(AIInsightType.ReengagementOpportunity, out var reeng))
            sections.Add(new OperationalSummarySection("Reactivación", $"{reeng} paciente(s) inactivos identificados.", reeng));

        var failedWorkflows = await _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId && e.Status == WorkflowExecutionStatus.Failed && e.CreatedAt >= start && e.CreatedAt < end)
            .CountAsync(cancellationToken);
        if (failedWorkflows > 0)
            sections.Add(new OperationalSummarySection("Automatizaciones", $"{failedWorkflows} ejecución(es) fallida(s).", failedWorkflows));

        var summary = critical > 0
            ? $"Resumen del día: {critical} alerta(s) crítica(s), {warning} advertencia(s). Requiere atención."
            : warning > 0
                ? $"Resumen del día: {warning} advertencia(s). Revisar recomendaciones."
                : insights.Count > 0
                    ? $"Resumen del día: {insights.Count} insight(s) generados. Sin alertas críticas."
                    : "Sin actividad de IA para esta fecha.";

        return new OperationalSummaryDto(
            date,
            summary,
            critical + warning,
            insights.Count(i => i.Status == AIInsightStatus.New),
            sections);
    }
}
