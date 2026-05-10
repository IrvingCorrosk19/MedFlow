using MedFlow.Domain.Enums;

namespace MedFlow.Application.PatientPortal;

public sealed record PatientPortalDashboardDto(
    PatientProfileSummaryDto Profile,
    PatientUpcomingAppointmentDto? NextAppointment,
    int PendingAppointmentsCount,
    decimal BalanceDue,
    PatientInvoiceSummaryDto? LastInvoice,
    PatientPaymentSummaryDto? LastPayment,
    int UnreadNotificationsCount,
    IReadOnlyList<PatientNotificationItemDto> RecentNotifications);

public sealed record PatientProfileSummaryDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    DateTime? DateOfBirth);

public sealed record PatientUpcomingAppointmentDto(
    Guid Id,
    DateTime ScheduledDate,
    TimeSpan StartTime,
    string DoctorName,
    string? Speciality,
    AppointmentStatus Status,
    string? Reason,
    string? ConsultationRoom);

public sealed record PatientInvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    DateTime IssueDate,
    decimal TotalAmount,
    decimal BalanceDue,
    InvoiceStatus Status);

public sealed record PatientPaymentSummaryDto(
    Guid Id,
    DateTime PaymentDate,
    decimal Amount,
    string? InvoiceNumber,
    string? ReferenceNumber);

public sealed record PatientNotificationItemDto(
    Guid Id,
    string Title,
    string? Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt);

public sealed record PatientProfileDto(
    Guid Id,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    DateTime? FechaNacimiento,
    string? Sexo,
    string? TipoDocumento,
    string? NumeroDocumento,
    string? Telefono,
    string? Correo,
    string? Direccion,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono,
    string? Alergias,
    string? Observaciones);

public sealed record PatientProfileUpdateDto(
    string? Telefono,
    string? Correo,
    string? Direccion,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono);

public sealed record PatientAppointmentListItemDto(
    Guid Id,
    Guid DoctorId,
    DateTime ScheduledDate,
    TimeSpan StartTime,
    string DoctorName,
    string? Speciality,
    AppointmentStatus Status,
    string? Reason,
    string? ConsultationRoom);

public sealed record PatientInvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    InvoiceStatus Status);

public sealed record PatientPaymentListItemDto(
    Guid Id,
    DateTime PaymentDate,
    decimal Amount,
    string? InvoiceNumber,
    ClinicalPaymentMethod PaymentMethod,
    string? ReferenceNumber,
    PaymentEntryStatus Status);

public sealed record PortalDoctorDto(
    Guid Id,
    string FullName,
    string? Speciality,
    string? ConsultationRoom,
    string? WorkingHours);

public sealed record PortalAppointmentRequestDto(
    Guid DoctorId,
    DateTime DesiredDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Reason);

public sealed record PortalPrescriptionDto(
    Guid Id,
    DateTime IssuedAt,
    string MedicationName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? Instructions,
    string? PrescriberName,
    bool IsVoid);

