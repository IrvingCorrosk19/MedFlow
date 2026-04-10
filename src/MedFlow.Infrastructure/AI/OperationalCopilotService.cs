using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Interfaces.AI.Providers;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class OperationalCopilotService : IOperationalCopilotService
{
    private readonly IApplicationDbContext _context;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIInsightService _insightService;
    private readonly IAIModelProvider _aiProvider;

    public OperationalCopilotService(
        IApplicationDbContext context,
        IAISettingsService aiSettings,
        IAIInsightService insightService,
        IAIModelProvider aiProvider)
    {
        _context = context;
        _aiSettings = aiSettings;
        _insightService = insightService;
        _aiProvider = aiProvider;
    }

    public async Task<CopilotResponse> QueryAsync(Guid tenantId, string query, CancellationToken cancellationToken = default)
    {
        if (!await _aiSettings.IsCopilotEnabledAsync(tenantId, cancellationToken))
            return new CopilotResponse("IA desactivada para este tenant.", [], []);

        // If a real LLM is available, use it with clinic context
        if (_aiProvider.IsAvailable)
        {
            return await QueryWithLLMAsync(tenantId, query, cancellationToken);
        }

        // Fallback: rule-based responses
        return await QueryRuleBasedAsync(tenantId, query, cancellationToken);
    }

    private async Task<CopilotResponse> QueryWithLLMAsync(Guid tenantId, string query, CancellationToken cancellationToken)
    {
        // Build a context-rich prompt with live clinic data
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Contexto de la clínica (datos en tiempo real):");

        try
        {
            var today = DateTime.UtcNow.Date;
            var todayApts = await _context.Appointments
                .AsNoTracking()
                .CountAsync(a => a.TenantId == tenantId
                    && a.Status == AppointmentStatus.Scheduled
                    && a.ScheduledDate >= today
                    && a.ScheduledDate < today.AddDays(1)
                    && !a.IsDeleted, cancellationToken);

            var pendingInvoices = await _context.BillingInvoices
                .AsNoTracking()
                .CountAsync(i => i.TenantId == tenantId
                    && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid)
                    && !i.IsDeleted, cancellationToken);

            var totalPatients = await _context.Patients
                .AsNoTracking()
                .CountAsync(p => p.TenantId == tenantId && p.IsActive && !p.IsDeleted, cancellationToken);

            contextBuilder.AppendLine($"- Citas programadas para hoy: {todayApts}");
            contextBuilder.AppendLine($"- Facturas pendientes de cobro: {pendingInvoices}");
            contextBuilder.AppendLine($"- Pacientes activos: {totalPatients}");

            // Recent AI insights
            var metrics = await _insightService.GetDashboardMetricsAsync(tenantId, DateTime.UtcNow.AddDays(-7), null, cancellationToken);
            contextBuilder.AppendLine($"- Insights IA (7 días): {metrics.TotalGenerated} generados, {metrics.CriticalCount} críticos");
        }
        catch { /* don't fail if context enrichment fails */ }

        var systemPrompt = "Eres un asistente médico-administrativo inteligente integrado en MedFlow, " +
            "un sistema de gestión clínica. Ayudas a los médicos y administradores a tomar mejores decisiones. " +
            "Responde siempre en español, de forma concisa, profesional y útil. " +
            "Si te preguntan sobre datos específicos de pacientes que no tienes, sugiere dónde encontrarlos en el sistema.";

        var fullPrompt = contextBuilder + "\n\nConsulta del usuario: " + query;

        var llmResponse = await _aiProvider.CompleteAsync(fullPrompt, new AIModelOptions(
            MaxTokens: 512,
            SystemPrompt: systemPrompt
        ), cancellationToken);

        if (llmResponse != null)
        {
            var suggestions = new List<string>
            {
                "¿Cuántas citas tengo hoy?",
                "¿Hay facturas vencidas?",
                "¿Qué pacientes necesitan seguimiento?"
            };
            return new CopilotResponse(llmResponse, [], suggestions);
        }

        // LLM failed, fall back to rule-based
        return await QueryRuleBasedAsync(tenantId, query, cancellationToken);
    }

    private async Task<CopilotResponse> QueryRuleBasedAsync(Guid tenantId, string query, CancellationToken cancellationToken)
    {
        var q = (query ?? "").Trim().ToLowerInvariant();
        var items = new List<CopilotResponseItem>();
        var suggestions = new List<string>();

        if (q.Contains("paciente") && (q.Contains("seguimiento") || q.Contains("inactivo") || q.Contains("riesgo")))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, InsightType: AIInsightType.ReengagementOpportunity, Status: AIInsightStatus.New, EntityType: "Patient", Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityId != null ? $"/Patients/Edit/{i.EntityId}" : null));
            suggestions.Add("Ver insights de reengagement");
        }
        else if (q.Contains("cita") && (q.Contains("riesgo") || q.Contains("no-show") || q.Contains("inasistencia")))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, AIInsightType.NoShowRisk, AIInsightStatus.New, MinScore: 50, EntityType: "Appointment", Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityId != null ? $"/Appointments/Edit/{i.EntityId}" : null));
            suggestions.Add("Ver citas con riesgo de inasistencia");
        }
        else if (q.Contains("factura") || q.Contains("cobro") || q.Contains("pago"))
        {
            var insights = await _insightService.ListAsync(new AIInsightFilter(tenantId, AIInsightType.PaymentRisk, AIInsightStatus.New, AISeverity.Critical, Page: 1, PageSize: 10), cancellationToken);
            foreach (var i in insights)
                items.Add(new CopilotResponseItem(i.Title, i.Summary, i.EntityType, i.EntityId, i.EntityType == "Patient" && i.EntityId != null ? $"/BillingInvoices?patientId={i.EntityId}" : null));
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
