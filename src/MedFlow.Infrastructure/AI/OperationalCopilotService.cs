using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class OperationalCopilotService : IOperationalCopilotService
{
    private readonly IApplicationDbContext _context;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIInsightService _insightService;

    public OperationalCopilotService(
        IApplicationDbContext context,
        IAISettingsService aiSettings,
        IAIInsightService insightService)
    {
        _context = context;
        _aiSettings = aiSettings;
        _insightService = insightService;
    }

    public async Task<CopilotResponse> QueryAsync(Guid tenantId, string query, CancellationToken cancellationToken = default)
    {
        if (!await _aiSettings.IsCopilotEnabledAsync(tenantId, cancellationToken))
            return new CopilotResponse("IA desactivada para este tenant.", [], []);

        var q = (query ?? "").Trim().ToLowerInvariant();
        var items = new List<CopilotResponseItem>();
        var suggestions = new List<string>();

        if (q.Contains("paciente") && (q.Contains("seguimiento") || q.Contains("inactivo") || q.Contains("riesgo")))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, InsightType: AIInsightType.ReengagementOpportunity, Status: AIInsightStatus.New, EntityType: "Patient", Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
            {
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityId != null ? $"/Patients/Edit/{i.EntityId}" : null));
            }
            suggestions.Add("Ver insights de reengagement");
        }
        else if (q.Contains("cita") && (q.Contains("riesgo") || q.Contains("no-show") || q.Contains("inasistencia")))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, AIInsightType.NoShowRisk, AIInsightStatus.New, MinScore: 50, EntityType: "Appointment", Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
            {
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityId != null ? $"/Appointments/Edit/{i.EntityId}" : null));
            }
            suggestions.Add("Ver citas con riesgo de inasistencia");
        }
        else if (q.Contains("factura") || q.Contains("cobro") || q.Contains("pago"))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, AIInsightType.PaymentRisk, AIInsightStatus.New, AISeverity.Critical, Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
            {
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityType == "Patient" && i.EntityId != null ? $"/BillingInvoices?patientId={i.EntityId}" : null));
            }
            suggestions.Add("Revisar facturas vencidas");
        }
        else
        {
            var metrics = await _insightService.GetDashboardMetricsAsync(tenantId, DateTime.UtcNow.AddDays(-7), null, cancellationToken);
            var summary = $"Últimos 7 días: {metrics.TotalGenerated} insights generados, {metrics.CriticalCount} críticos, {metrics.NewCount} pendientes de revisión.";
            items.Add(new CopilotResponseItem("Resumen IA", summary, null, null, "/AI/Insights"));
            suggestions.Add("¿Qué pacientes requieren seguimiento?");
            suggestions.Add("¿Qué citas tienen alto riesgo?");
            suggestions.Add("¿Qué facturas requieren gestión?");
            return new CopilotResponse(summary, items, suggestions);
        }

        var sum = items.Count > 0
            ? $"Se encontraron {items.Count} elemento(s) relevantes."
            : "No hay elementos que coincidan con la consulta.";
        return new CopilotResponse(sum, items, suggestions);
    }
}
