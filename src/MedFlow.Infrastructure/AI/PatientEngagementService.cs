using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class PatientEngagementService : IPatientEngagementService
{
    private readonly IApplicationDbContext _context;

    public PatientEngagementService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PatientEngagementResult> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var factors = new List<string>();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {patientId} not found");

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId && !a.IsDeleted)
            .OrderByDescending(a => a.ScheduledDate)
            .ToListAsync(cancellationToken);

        var lastAppointment = appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.ScheduledDate)
            .FirstOrDefault();

        var lastAppointmentDate = lastAppointment?.ScheduledDate;
        var appointmentsLast90 = appointments.Count(a =>
            a.ScheduledDate >= now.AddDays(-90) &&
            (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed));

        var hasPortal = !string.IsNullOrEmpty(patient.UserId);
        var confirmedRecent = appointments.Any(a =>
            a.ScheduledDate >= now.AddDays(-30) &&
            a.Status == AppointmentStatus.Confirmed);

        var score = 50m;

        if (lastAppointmentDate.HasValue)
        {
            var daysSinceLast = (now.Date - lastAppointmentDate.Value.Date).TotalDays;
            if (daysSinceLast <= 30)
            {
                score += 25;
                factors.Add("Visita reciente (últimos 30 días)");
            }
            else if (daysSinceLast <= 90)
            {
                score += 15;
                factors.Add("Visita en últimos 90 días");
            }
            else if (daysSinceLast > 180)
            {
                score -= 20;
                factors.Add("Sin visita en más de 6 meses");
            }
        }
        else
        {
            score -= 30;
            factors.Add("Sin citas completadas registradas");
        }

        if (hasPortal)
        {
            score += 10;
            factors.Add("Acceso al portal del paciente");
        }

        if (confirmedRecent)
        {
            score += 10;
            factors.Add("Confirma citas recientemente");
        }

        if (appointmentsLast90 >= 3)
        {
            score += 5;
            factors.Add("Alta frecuencia de atención");
        }

        var noShowRecent = appointments.Any(a =>
            a.ScheduledDate >= now.AddDays(-90) &&
            a.Status == AppointmentStatus.NoShow);
        if (noShowRecent)
        {
            score -= 15;
            factors.Add("No-show reciente");
        }

        score = Math.Max(0, Math.Min(score, 100));

        var level = score >= 75 ? PatientEngagementLevel.HighlyEngaged
            : score >= 50 ? PatientEngagementLevel.MediumEngagement
            : score >= 30 ? PatientEngagementLevel.AtRisk
            : PatientEngagementLevel.LostInactive;

        var summary = level switch
        {
            PatientEngagementLevel.HighlyEngaged => "Paciente muy comprometido con alta actividad.",
            PatientEngagementLevel.MediumEngagement => "Compromiso medio. Monitoreo rutinario.",
            PatientEngagementLevel.AtRisk => "Riesgo de deserción. Sugerir seguimiento.",
            _ => "Paciente inactivo. Oportunidad de reactivación."
        };

        return new PatientEngagementResult(
            level,
            score,
            summary,
            factors,
            lastAppointmentDate,
            appointmentsLast90,
            hasPortal,
            confirmedRecent);
    }
}
