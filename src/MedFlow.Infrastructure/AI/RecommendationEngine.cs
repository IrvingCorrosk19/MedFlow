using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class RecommendationEngine : IRecommendationEngine
{
    private readonly IApplicationDbContext _context;
    private readonly INoShowRiskService _noShowRisk;
    private readonly IPaymentRiskService _paymentRisk;
    private readonly IPatientEngagementService _engagement;
    private readonly IAISettingsService _aiSettings;
    private readonly ISubscriptionLimitService _limits;

    public RecommendationEngine(
        IApplicationDbContext context,
        INoShowRiskService noShowRisk,
        IPaymentRiskService paymentRisk,
        IPatientEngagementService engagement,
        IAISettingsService aiSettings,
        ISubscriptionLimitService limits)
    {
        _context = context;
        _noShowRisk = noShowRisk;
        _paymentRisk = paymentRisk;
        _engagement = engagement;
        _aiSettings = aiSettings;
        _limits = limits;
    }

    public async Task<IReadOnlyList<AIRecommendation>> GenerateRecommendationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var recommendations = new List<AIRecommendation>();

        if (!await _aiSettings.IsEnabledAsync(tenantId, cancellationToken))
            return recommendations;

        var now = DateTime.UtcNow;
        var today = now.Date;

        if (await _aiSettings.IsNoShowEnabledAsync(tenantId, cancellationToken))
        {
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.TenantId == tenantId
                    && a.ScheduledDate >= today
                    && a.ScheduledDate <= today.AddDays(2)
                    && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                    && !a.IsDeleted)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var apt in upcomingAppointments)
            {
                var risk = await _noShowRisk.EvaluateAsync(apt.Id, cancellationToken);
                if (risk.Score >= 50)
                {
                    recommendations.Add(new AIRecommendation(
                        "NoShowRisk",
                        $"Riesgo de inasistencia: {apt.Patient?.NombreCompleto ?? "Paciente"}",
                        risk.Summary,
                        risk.Recommendation,
                        "Appointment",
                        apt.Id.ToString(),
                        $"/Appointments/Edit/{apt.Id}",
                        risk.Score >= 70 ? 1 : 2,
                        JsonSerializer.Serialize(new { risk.Factors, risk.Score, risk.Confidence }),
                        risk.Score >= 70 ? "Critical" : "Warning"));
                }
            }
        }

        if (await _aiSettings.IsPaymentRiskEnabledAsync(tenantId, cancellationToken))
        {
            var patientsWithOverdue = await _context.BillingInvoices
                .Where(i => i.TenantId == tenantId && i.BalanceDue > 0 && i.DueDate < today && !i.IsDeleted)
                .Select(i => i.PatientId)
                .Distinct()
                .Take(30)
                .ToListAsync(cancellationToken);

            foreach (var patientId in patientsWithOverdue)
            {
                var risk = await _paymentRisk.EvaluatePatientAsync(patientId, cancellationToken);
                if (risk.Score >= 40)
                {
                    recommendations.Add(new AIRecommendation(
                        "PaymentRisk",
                        $"Riesgo de cobro: paciente con facturas vencidas",
                        risk.Summary,
                        risk.Recommendation,
                        "Patient",
                        patientId.ToString(),
                        $"/BillingInvoices?patientId={patientId}",
                        risk.Score >= 60 ? 1 : 2,
                        JsonSerializer.Serialize(new { risk.OverdueInvoicesCount, risk.TotalOverdueAmount }),
                        risk.Severity));
                }
            }
        }

        if (await _aiSettings.IsRecommendationsEnabledAsync(tenantId, cancellationToken))
        {
            var inactivePatients = await _context.Patients
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .Select(p => p.Id)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var patientId in inactivePatients)
            {
                var engagement = await _engagement.EvaluateAsync(patientId, cancellationToken);
                if (engagement.Level == PatientEngagementLevel.LostInactive || engagement.Level == PatientEngagementLevel.AtRisk)
                {
                    recommendations.Add(new AIRecommendation(
                        "ReengagementOpportunity",
                        "Paciente inactivo o en riesgo de deserción",
                        engagement.Summary,
                        "Contactar para seguimiento y reactivación",
                        "Patient",
                        patientId.ToString(),
                        $"/Patients/Edit/{patientId}",
                        3,
                        JsonSerializer.Serialize(new { engagement.Score, engagement.LastAppointmentDate }),
                        engagement.Level == PatientEngagementLevel.LostInactive ? "Warning" : "Info"));
                }
            }
        }

        if (await _aiSettings.GetAllowOperationalSuggestionsAsync(tenantId, cancellationToken))
        {
            var thirtyDaysAgo = today.AddDays(-30);
            var doctorStats = await _context.Appointments
                .Where(a => a.TenantId == tenantId && a.ScheduledDate >= thirtyDaysAgo && !a.IsDeleted)
                .GroupBy(a => a.DoctorId)
                .Select(g => new { DoctorId = g.Key, Total = g.Count(), Cancelled = g.Count(a => a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow) })
                .Where(x => x.Total >= 5)
                .ToListAsync(cancellationToken);

            foreach (var ds in doctorStats.Where(x => x.Total > 0 && (x.Cancelled * 100.0 / x.Total) >= 30))
            {
                var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == ds.DoctorId, cancellationToken);
                recommendations.Add(new AIRecommendation(
                    "OperationalAnomaly",
                    $"Doctor con alta tasa de cancelaciones: {doctor?.FullName ?? "Doctor"} ({ds.Cancelled}/{ds.Total} = {ds.Cancelled * 100 / ds.Total}%)",
                    "Revisar franjas horarias y políticas de reprogramación.",
                    "Analizar causas y considerar recordatorios reforzados.",
                    "Doctor",
                    ds.DoctorId.ToString(),
                    $"/Doctors/Edit/{ds.DoctorId}",
                    2,
                    JsonSerializer.Serialize(new { ds.Cancelled, ds.Total, RatePercent = ds.Cancelled * 100.0 / ds.Total }),
                    "Warning"));
            }

            var last7 = today.AddDays(-7);
            var prev7 = today.AddDays(-14);
            var cancelLast7 = await _context.Appointments.CountAsync(a => a.TenantId == tenantId && a.ScheduledDate >= last7 && a.ScheduledDate < today && (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow) && !a.IsDeleted, cancellationToken);
            var cancelPrev7 = await _context.Appointments.CountAsync(a => a.TenantId == tenantId && a.ScheduledDate >= prev7 && a.ScheduledDate < last7 && (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow) && !a.IsDeleted, cancellationToken);
            if (cancelPrev7 > 0 && cancelLast7 > cancelPrev7 * 1.5m)
            {
                recommendations.Add(new AIRecommendation(
                    "OperationalAnomaly",
                    "Aumento inusual de citas canceladas",
                    $"Últimos 7 días: {cancelLast7} cancelaciones vs {cancelPrev7} en la semana anterior (+{((cancelLast7 - cancelPrev7) * 100 / cancelPrev7)}%).",
                    "Revisar recordatorios, horarios y causas de cancelación.",
                    "Tenant",
                    tenantId.ToString(),
                    "/AI/AIDashboard",
                    2,
                    JsonSerializer.Serialize(new { cancelLast7, cancelPrev7 }),
                    "Warning"));
            }

            var usage = await _limits.GetCurrentUsageAsync(tenantId, cancellationToken);
            var nearLimitItems = new List<string>();
            if (usage.UsersUsagePercent >= 80 && usage.MaxUsers > 0) nearLimitItems.Add($"Usuarios ({usage.Users} de {usage.MaxUsers})");
            if (usage.DoctorsUsagePercent >= 80 && usage.MaxDoctors > 0) nearLimitItems.Add($"Doctores ({usage.Doctors} de {usage.MaxDoctors})");
            if (usage.PatientsUsagePercent >= 80 && usage.MaxPatients > 0) nearLimitItems.Add($"Pacientes ({usage.Patients} de {usage.MaxPatients})");
            if (usage.AppointmentsUsagePercent >= 80 && usage.MaxAppointmentsPerMonth > 0) nearLimitItems.Add($"Citas del mes ({usage.AppointmentsThisMonth} de {usage.MaxAppointmentsPerMonth})");
            if (nearLimitItems.Count > 0)
            {
                recommendations.Add(new AIRecommendation(
                    "OperationalAnomaly",
                    "Tenant cerca del límite del plan",
                    string.Join("; ", nearLimitItems),
                    "Considerar actualizar el plan de suscripción.",
                    "Tenant",
                    tenantId.ToString(),
                    "/TenantBilling",
                    3,
                    JsonSerializer.Serialize(new { nearLimitItems }),
                    "Info"));
            }
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }
}
