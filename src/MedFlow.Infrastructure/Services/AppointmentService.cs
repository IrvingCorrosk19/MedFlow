using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MedFlow.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly IClinicalUserScope _clinicalScope;
    private readonly ISubscriptionLimitService _limits;
    private readonly IEventLogService _eventLog;
    private readonly IAuditLogService _audit;
    private readonly INotificationDispatchService _notifications;

    public AppointmentService(
        IApplicationDbContext context,
        ITenantContext tenant,
        IClinicalUserScope clinicalScope,
        ISubscriptionLimitService limits,
        IEventLogService eventLog,
        IAuditLogService audit,
        INotificationDispatchService notifications)
    {
        _context = context;
        _tenant = tenant;
        _clinicalScope = clinicalScope;
        _limits = limits;
        _eventLog = eventLog;
        _audit = audit;
        _notifications = notifications;
    }

    private async Task<IQueryable<Appointment>> ApplySoloDoctorAppointmentsAsync(IQueryable<Appointment> query, CancellationToken cancellationToken)
    {
        var (restrict, docId) = await _clinicalScope.GetDoctorDataScopeAsync(cancellationToken).ConfigureAwait(false);
        if (!restrict)
            return query;
        if (!docId.HasValue)
            return query.Where(a => false);
        return query.Where(a => a.DoctorId == docId.Value);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = ClinicalOperationalTenantScope.ApplyToAppointments(_tenant, _context.Appointments.AsNoTracking());
        query = await ApplySoloDoctorAppointmentsAsync(query, cancellationToken).ConfigureAwait(false);
        return await query
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var baseQ = ClinicalOperationalTenantScope.ApplyToAppointments(_tenant, _context.Appointments.AsNoTracking());
        baseQ = await ApplySoloDoctorAppointmentsAsync(baseQ, cancellationToken).ConfigureAwait(false);
        return await baseQ
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDate == date && !a.IsDeleted)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(DateTime? from = null, DateTime? to = null, Guid? doctorId = null, Guid? patientId = null, CancellationToken cancellationToken = default)
    {
        var query = ClinicalOperationalTenantScope.ApplyToAppointments(_tenant, _context.Appointments.AsNoTracking())
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => !a.IsDeleted);
        query = await ApplySoloDoctorAppointmentsAsync(query, cancellationToken).ConfigureAwait(false);

        if (from.HasValue)
            query = query.Where(a => a.ScheduledDate >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.ScheduledDate <= to.Value);
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        return await query.OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime).Take(2000).ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var tid = appointment.TenantId != Guid.Empty ? appointment.TenantId : _tenant.TenantId;
        if (!tid.HasValue)
            return (false, "No se pudo determinar la clínica para validar límites del plan.");

        var (scopeDoctor, linkedDoctorId) = await _clinicalScope.GetDoctorDataScopeAsync(cancellationToken).ConfigureAwait(false);
        if (scopeDoctor && (!linkedDoctorId.HasValue || appointment.DoctorId != linkedDoctorId.Value))
            return (false, "No puede crear citas para otro médico.");

        var limit = await _limits.CanCreateAppointmentAsync(tid.Value, cancellationToken);
        if (!limit.Allowed)
        {
            var msg = limit.Suggestion != null ? $"{limit.Message} {limit.Suggestion}" : limit.Message;
            return (false, msg);
        }

        // Use execution strategy + serializable transaction to prevent double-booking race condition
        var strategy = _context.Database.CreateExecutionStrategy();
        bool conflictInTx = false;
        await strategy.ExecuteAsync(async () =>
        {
            // IsolationLevel.Serializable is only supported by relational providers (e.g. PostgreSQL)
            var useIsolation = _context.Database.IsRelational();
            var tx = useIsolation
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : await _context.Database.BeginTransactionAsync(cancellationToken);
            await using (tx)
            {
                conflictInTx = await HasConflictAsync(appointment.DoctorId, appointment.ScheduledDate, appointment.StartTime, appointment.EndTime, null, cancellationToken);
                if (!conflictInTx)
                {
                    await _context.Appointments.AddAsync(appointment, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                else
                {
                    await tx.RollbackAsync(cancellationToken);
                }
            }
        });
        if (conflictInTx)
            return (false, "Ya existe una cita en ese horario para el doctor seleccionado.");

        await _eventLog.EnqueueAsync("AppointmentCreated", new
        {
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.ScheduledDate,
            StartTime = appointment.StartTime.ToString(),
            EndTime = appointment.EndTime.ToString(),
            appointment.Reason
        }, "Appointment", appointment.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("Create", "Appointments", nameof(Appointment), appointment.Id.ToString(),
            "Cita creada"), cancellationToken);

        // Fire confirmation notification (best-effort)
        _ = DispatchAppointmentNotificationAsync(
            appointment, NotificationEventType.AppointmentConfirmed, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var (scopeDoctor, linkedDoctorId) = await _clinicalScope.GetDoctorDataScopeAsync(cancellationToken).ConfigureAwait(false);
        if (scopeDoctor && (!linkedDoctorId.HasValue || appointment.DoctorId != linkedDoctorId.Value))
            return (false, "No puede gestionar citas de otro médico.");

        var conflict = await HasConflictAsync(appointment.DoctorId, appointment.ScheduledDate, appointment.StartTime, appointment.EndTime, appointment.Id, cancellationToken);
        if (conflict)
            return (false, "Ya existe una cita en ese horario para el doctor seleccionado.");

        var previous = await GetByIdAsync(appointment.Id, cancellationToken);

        appointment.UpdatedAt = DateTime.UtcNow;
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        if (previous != null && previous.Status != AppointmentStatus.Confirmed && appointment.Status == AppointmentStatus.Confirmed)
        {
            await _eventLog.EnqueueAsync("AppointmentConfirmed", new
            {
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.ScheduledDate,
                StartTime = appointment.StartTime.ToString(),
                EndTime = appointment.EndTime.ToString()
            }, "Appointment", appointment.Id.ToString(), cancellationToken);

            _ = DispatchAppointmentNotificationAsync(
                appointment, NotificationEventType.AppointmentConfirmed, cancellationToken);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            await _eventLog.EnqueueAsync("AppointmentCancelled", new
            {
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.ScheduledDate
            }, "Appointment", appointment.Id.ToString(), cancellationToken);

            _ = DispatchAppointmentNotificationAsync(
                appointment, NotificationEventType.AppointmentCancelled, cancellationToken);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
            await _audit.LogAsync(new AuditLogWriteDto("Cancel", "Appointments", nameof(Appointment), appointment.Id.ToString(),
                "Cita cancelada"), cancellationToken);
        else
            await _audit.LogAsync(new AuditLogWriteDto("Update", "Appointments", nameof(Appointment), appointment.Id.ToString(),
                "Cita actualizada"), cancellationToken);

        return (true, null);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apt = await GetByIdAsync(id, cancellationToken);
        if (apt == null) return;

        apt.IsDeleted = true;
        apt.UpdatedAt = DateTime.UtcNow;
        _context.Appointments.Update(apt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid doctorId, DateTime date, TimeSpan start, TimeSpan end, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = ClinicalOperationalTenantScope.ApplyToAppointments(_tenant, _context.Appointments)
            .Where(a => a.DoctorId == doctorId && a.ScheduledDate == date
                        && !a.IsDeleted && a.Status != AppointmentStatus.Cancelled
                        && start < a.EndTime && end > a.StartTime);
        query = await ApplySoloDoctorAppointmentsAsync(query, cancellationToken).ConfigureAwait(false);

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    private async Task DispatchAppointmentNotificationAsync(
        Appointment appointment,
        NotificationEventType eventType,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = appointment.TenantId != Guid.Empty ? appointment.TenantId : (_tenant.TenantId ?? Guid.Empty);
            if (tenantId == Guid.Empty) return;

            // Load patient email if not already present
            var patient = appointment.Patient
                ?? await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == appointment.PatientId, cancellationToken);
            var doctor = appointment.Doctor
                ?? await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == appointment.DoctorId, cancellationToken);

            var payload = new Dictionary<string, object>
            {
                ["patient_name"]       = patient?.NombreCompleto ?? "",
                ["doctor_name"]        = doctor?.FullName ?? "",
                ["appointment_date"]   = appointment.ScheduledDate.ToString("dd/MM/yyyy"),
                ["appointment_time"]   = appointment.StartTime.ToString(@"hh\:mm"),
                ["appointment_end"]    = appointment.EndTime.ToString(@"hh\:mm"),
                ["appointment_reason"] = appointment.Reason ?? ""
            };

            var request = new DispatchRequest(
                TenantId: tenantId,
                EventType: eventType,
                Payload: payload,
                RecipientEmail: patient?.Correo,
                RecipientPhone: patient?.Telefono,
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString());

            await _notifications.DispatchAsync(request, cancellationToken);
        }
        catch
        {
            // Notifications are best-effort — never block the main flow
        }
    }
}
