using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class NoShowRiskService : INoShowRiskService
{
    private readonly IApplicationDbContext _context;

    public NoShowRiskService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NoShowRiskResult> EvaluateAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found");

        if (appointment.Status != AppointmentStatus.Scheduled && appointment.Status != AppointmentStatus.Confirmed)
            return new NoShowRiskResult(0, "N/A", "Cita no elegible para evaluación", "", [], 0);

        var factors = new List<string>();
        var score = 0m;
        var patientId = appointment.PatientId;

        var patientAppointments = await _context.Appointments
            .Where(a => a.PatientId == patientId && a.Id != appointmentId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        var noShowCount = patientAppointments.Count(a => a.Status == AppointmentStatus.NoShow);
        var cancelCount = patientAppointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        var totalPast = patientAppointments.Count(a => a.ScheduledDate < DateTime.UtcNow.Date);
        var completedCount = patientAppointments.Count(a => a.Status == AppointmentStatus.Completed);

        if (noShowCount > 0)
        {
            var noShowRate = totalPast > 0 ? (decimal)noShowCount / totalPast : 0;
            score += Math.Min(noShowRate * 40, 40);
            factors.Add($"{noShowCount} no-show(s) en historial");
        }

        if (cancelCount >= 2)
        {
            score += Math.Min(cancelCount * 8, 25);
            factors.Add($"{cancelCount} cancelaciones previas");
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            score += 15;
            factors.Add("Cita sin confirmar");
        }

        var daysToAppointment = (appointment.ScheduledDate - DateTime.UtcNow.Date).Days;
        var daysSinceCreation = (DateTime.UtcNow - appointment.CreatedAt).Days;
        if (daysToAppointment <= 1 && daysSinceCreation < 2)
        {
            score += 10;
            factors.Add("Cita creada con poco tiempo de anticipación");
        }

        var hour = appointment.StartTime.Hours;
        if (hour < 8 || hour >= 18)
        {
            score += 5;
            factors.Add("Horario en franja históricamente problemática");
        }

        var overdueInvoices = await _context.BillingInvoices
            .Where(i => i.PatientId == patientId && i.BalanceDue > 0 && i.Status != InvoiceStatus.Cancelled && !i.IsDeleted)
            .CountAsync(cancellationToken);
        if (overdueInvoices > 0)
        {
            score += Math.Min(overdueInvoices * 5, 15);
            factors.Add($"Deuda pendiente ({overdueInvoices} factura(s))");
        }

        if (completedCount == 0 && totalPast > 0)
        {
            score += 10;
            factors.Add("Paciente sin citas completadas previas");
        }

        score = Math.Min(score, 100);

        var riskLevel = score >= 60 ? "Alto" : score >= 35 ? "Medio" : "Bajo";
        var confidence = totalPast >= 3 ? 0.85m : totalPast >= 1 ? 0.7m : 0.5m;

        var summary = score >= 60
            ? $"Riesgo alto de inasistencia (score {score:F0}/100). {string.Join("; ", factors)}."
            : score >= 35
                ? $"Riesgo moderado de inasistencia (score {score:F0}/100)."
                : $"Riesgo bajo de inasistencia (score {score:F0}/100).";

        var recommendation = score >= 60
            ? "Enviar recordatorio reforzado y solicitar confirmación manual. Considerar contacto telefónico."
            : score >= 35
                ? "Enviar recordatorio estándar y solicitar confirmación."
                : "Recordatorio estándar según configuración.";

        return new NoShowRiskResult(score, riskLevel, summary, recommendation, factors, confidence);
    }
}
