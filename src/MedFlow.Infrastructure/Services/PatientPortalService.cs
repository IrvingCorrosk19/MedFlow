using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Application.PatientPortal;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AuditLogWriteDto = MedFlow.Application.Interfaces.AuditLogWriteDto;

namespace MedFlow.Infrastructure.Services;

public sealed class PatientPortalService : IPatientPortalService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public PatientPortalService(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PatientPortalOptions> GetOptionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await _db.TenantSettings.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.SettingKey.StartsWith("patient_portal."))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, cancellationToken);

        bool GetBool(string key, bool defaultValue) =>
            settings.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : defaultValue;

        return new PatientPortalOptions
        {
            Enabled = GetBool("patient_portal.enabled", true),
            AllowAppointmentConfirmation = GetBool("patient_portal.allow_appointment_confirmation", true),
            AllowAppointmentCancellation = GetBool("patient_portal.allow_appointment_cancellation", true),
            AllowProfileEdit = GetBool("patient_portal.allow_profile_edit", true),
            ShowMedicalSummary = GetBool("patient_portal.show_medical_summary", false),
            ShowPrescriptions = GetBool("patient_portal.show_prescriptions", false),
            ShowBilling = GetBool("patient_portal.show_billing", true),
            ShowNotifications = GetBool("patient_portal.show_notifications", true)
        };
    }

    public async Task<PatientPortalDashboardDto?> GetDashboardAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await GetPatientWithTenantAsync(patientId, cancellationToken);
        if (patient == null) return null;

        var profile = ToProfileSummary(patient);

        var now = DateTime.UtcNow;
        var nextApp = await _db.Appointments
            .Where(a => a.PatientId == patientId && !a.IsDeleted && a.ScheduledDate >= now.Date
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .Include(a => a.Doctor)
            .OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingCount = await _db.Appointments
            .CountAsync(a => a.PatientId == patientId && !a.IsDeleted && a.ScheduledDate >= now.Date
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow, cancellationToken);

        var balanceDue = await _db.BillingInvoices
            .Where(i => i.PatientId == patientId && !i.IsDeleted && i.BalanceDue > 0)
            .SumAsync(i => i.BalanceDue, cancellationToken);

        var lastInvoice = await _db.BillingInvoices
            .Where(i => i.PatientId == patientId && !i.IsDeleted)
            .OrderByDescending(i => i.IssueDate)
            .FirstOrDefaultAsync(cancellationToken);

        var lastPayment = await _db.Payments
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .Include(p => p.BillingInvoice)
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync(cancellationToken);

        var unreadCount = await _db.Notifications
            .CountAsync(n => n.TenantId == patient.TenantId && !n.IsDeleted && n.PatientId == patientId.ToString() && !n.IsRead, cancellationToken);

        var recentNotifs = await _db.Notifications
            .Where(n => n.TenantId == patient.TenantId && !n.IsDeleted && n.PatientId == patientId.ToString())
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new PatientNotificationItemDto(n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PatientPortalDashboardDto(
            profile,
            nextApp == null ? null : ToAppointmentDto(nextApp),
            pendingCount,
            balanceDue,
            lastInvoice == null ? null : ToInvoiceSummary(lastInvoice),
            lastPayment == null ? null : new PatientPaymentSummaryDto(lastPayment.Id, lastPayment.PaymentDate, lastPayment.Amount, lastPayment.BillingInvoice.InvoiceNumber, lastPayment.ReferenceNumber),
            unreadCount,
            recentNotifs);
    }

    public async Task<PatientProfileDto?> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var p = await GetPatientWithTenantAsync(patientId, cancellationToken);
        if (p == null) return null;

        return new PatientProfileDto(p.Id, p.PrimerNombre, p.SegundoNombre, p.PrimerApellido, p.SegundoApellido,
            p.FechaNacimiento, p.Sexo, p.TipoDocumento, p.NumeroDocumento, p.Telefono, p.Correo, p.Direccion,
            p.ContactoEmergenciaNombre, p.ContactoEmergenciaTelefono, p.Alergias, p.Observaciones);
    }

    public async Task UpdateProfileAsync(Guid patientId, PatientProfileUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var p = await _db.Patients.FirstOrDefaultAsync(x => x.Id == patientId && !x.IsDeleted, cancellationToken);
        if (p == null) throw new InvalidOperationException("Paciente no encontrado.");

        if (dto.Telefono != null) p.Telefono = dto.Telefono;
        if (dto.Correo != null) p.Correo = dto.Correo;
        if (dto.Direccion != null) p.Direccion = dto.Direccion;
        if (dto.ContactoEmergenciaNombre != null) p.ContactoEmergenciaNombre = dto.ContactoEmergenciaNombre;
        if (dto.ContactoEmergenciaTelefono != null) p.ContactoEmergenciaTelefono = dto.ContactoEmergenciaTelefono;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "PatientPortal", nameof(Patient), p.Id.ToString(), "Perfil actualizado por paciente"), cancellationToken);
    }

    public async Task<IReadOnlyList<PatientAppointmentListItemDto>> GetUpcomingAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Appointments
            .Where(a => a.PatientId == patientId && !a.IsDeleted && a.ScheduledDate >= now.Date
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .Include(a => a.Doctor)
            .OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime)
            .Select(a => new PatientAppointmentListItemDto(a.Id, a.ScheduledDate, a.StartTime, a.Doctor.FirstName + " " + a.Doctor.LastName, a.Doctor.Speciality, a.Status, a.Reason, a.ConsultationRoom))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientAppointmentListItemDto>> GetAppointmentHistoryAsync(Guid patientId, int take = 50, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Appointments
            .Where(a => a.PatientId == patientId && !a.IsDeleted && (a.ScheduledDate < now.Date || a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow))
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.ScheduledDate).ThenByDescending(a => a.StartTime)
            .Take(take)
            .Select(a => new PatientAppointmentListItemDto(a.Id, a.ScheduledDate, a.StartTime, a.Doctor.FirstName + " " + a.Doctor.LastName, a.Doctor.Speciality, a.Status, a.Reason, a.ConsultationRoom))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientAppointmentListItemDto?> GetAppointmentAsync(Guid patientId, Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var a = await _db.Appointments
            .Where(x => x.Id == appointmentId && x.PatientId == patientId && !x.IsDeleted)
            .Include(x => x.Doctor)
            .FirstOrDefaultAsync(cancellationToken);
        if (a == null) return null;
        return new PatientAppointmentListItemDto(a.Id, a.ScheduledDate, a.StartTime, a.Doctor.FullName, a.Doctor.Speciality, a.Status, a.Reason, a.ConsultationRoom);
    }

    public async Task<bool> ConfirmAppointmentAsync(Guid patientId, Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId && x.PatientId == patientId && !x.IsDeleted, cancellationToken);
        if (a == null) return false;
        if (a.Status != AppointmentStatus.Scheduled) return false;

        a.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Confirm", "PatientPortal", nameof(Appointment), a.Id.ToString(), "Cita confirmada por paciente"), cancellationToken);
        return true;
    }

    public async Task<bool> CancelAppointmentAsync(Guid patientId, Guid appointmentId, string? reason, CancellationToken cancellationToken = default)
    {
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId && x.PatientId == patientId && !x.IsDeleted, cancellationToken);
        if (a == null) return false;
        if (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.Completed) return false;

        a.Status = AppointmentStatus.Cancelled;
        a.Notes = string.IsNullOrWhiteSpace(a.Notes) ? $"Cancelado por paciente: {reason}" : a.Notes + $" | Cancelado por paciente: {reason}";
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Cancel", "PatientPortal", nameof(Appointment), a.Id.ToString(), "Cita cancelada por paciente"), cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PatientInvoiceListItemDto>> GetInvoicesAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.BillingInvoices
            .Where(i => i.PatientId == patientId && !i.IsDeleted)
            .OrderByDescending(i => i.IssueDate)
            .Select(i => new PatientInvoiceListItemDto(i.Id, i.InvoiceNumber, i.IssueDate, i.DueDate, i.TotalAmount, i.AmountPaid, i.BalanceDue, i.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientInvoiceListItemDto?> GetInvoiceAsync(Guid patientId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var i = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.Id == invoiceId && x.PatientId == patientId && !x.IsDeleted, cancellationToken);
        return i == null ? null : new PatientInvoiceListItemDto(i.Id, i.InvoiceNumber, i.IssueDate, i.DueDate, i.TotalAmount, i.AmountPaid, i.BalanceDue, i.Status);
    }

    public async Task<IReadOnlyList<PatientPaymentListItemDto>> GetPaymentsAsync(Guid patientId, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _db.Payments
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .Include(p => p.BillingInvoice)
            .OrderByDescending(p => p.PaymentDate)
            .Take(take)
            .Select(p => new PatientPaymentListItemDto(p.Id, p.PaymentDate, p.Amount, p.BillingInvoice.InvoiceNumber, p.PaymentMethod, p.ReferenceNumber, p.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<(decimal BalanceDue, decimal TotalPaid)> GetAccountStatusAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var balanceDue = await _db.BillingInvoices
            .Where(i => i.PatientId == patientId && !i.IsDeleted && i.BalanceDue > 0)
            .SumAsync(i => i.BalanceDue, cancellationToken);

        var totalPaid = await _db.Payments
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .SumAsync(p => p.Amount, cancellationToken);

        return (balanceDue, totalPaid);
    }

    public async Task<IReadOnlyList<PatientNotificationItemDto>> GetNotificationsAsync(Guid patientId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);
        if (patient == null) return [];

        return await _db.Notifications
            .Where(n => n.TenantId == patient.TenantId && !n.IsDeleted && n.PatientId == patientId.ToString())
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip).Take(take)
            .Select(n => new PatientNotificationItemDto(n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadNotificationsCountAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .CountAsync(n => !n.IsDeleted && n.PatientId == patientId.ToString() && !n.IsRead, cancellationToken);
    }

    public async Task MarkNotificationReadAsync(Guid patientId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.PatientId == patientId.ToString() && !x.IsDeleted, cancellationToken);
        if (n != null)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Guid?> GetPatientIdByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var p = await _db.Patients.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return p == Guid.Empty ? null : p;
    }

    private async Task<Patient?> GetPatientWithTenantAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Patients.Include(p => p.Tenant).FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);

    private static PatientProfileSummaryDto ToProfileSummary(Patient p) => new(
        p.Id, p.NombreCompleto, p.Correo, p.Telefono, p.FechaNacimiento);

    private static PatientUpcomingAppointmentDto ToAppointmentDto(Appointment a) => new(
        a.Id, a.ScheduledDate, a.StartTime, a.Doctor.FullName, a.Doctor.Speciality, a.Status, a.Reason, a.ConsultationRoom);

    private static PatientInvoiceSummaryDto ToInvoiceSummary(BillingInvoice i) => new(
        i.Id, i.InvoiceNumber, i.IssueDate, i.TotalAmount, i.BalanceDue, i.Status);
}
