using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionLimitService _limits;
    private readonly IEventLogService _eventLog;
    private readonly IAuditLogService _audit;

    public AppointmentService(
        IApplicationDbContext context,
        ITenantContext tenant,
        ISubscriptionLimitService limits,
        IEventLogService eventLog,
        IAuditLogService audit)
    {
        _context = context;
        _tenant = tenant;
        _limits = limits;
        _eventLog = eventLog;
        _audit = audit;
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDate == date && !a.IsDeleted)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(DateTime? from = null, DateTime? to = null, Guid? doctorId = null, Guid? patientId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => !a.IsDeleted);

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

        var limit = await _limits.CanCreateAppointmentAsync(tid.Value, cancellationToken);
        if (!limit.Allowed)
        {
            var msg = limit.Suggestion != null ? $"{limit.Message} {limit.Suggestion}" : limit.Message;
            return (false, msg);
        }

        var conflict = await HasConflictAsync(appointment.DoctorId, appointment.ScheduledDate, appointment.StartTime, appointment.EndTime, null, cancellationToken);
        if (conflict)
            return (false, "Ya existe una cita en ese horario para el doctor seleccionado.");

        await _context.Appointments.AddAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

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

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
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
        var query = _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.ScheduledDate == date
                        && !a.IsDeleted && a.Status != AppointmentStatus.Cancelled
                        && start < a.EndTime && end > a.StartTime);

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
