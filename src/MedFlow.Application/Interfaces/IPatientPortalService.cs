using MedFlow.Application.Options;
using MedFlow.Application.PatientPortal;

namespace MedFlow.Application.Interfaces;

public interface IPatientPortalService
{
    Task<PatientPortalOptions> GetOptionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PatientPortalDashboardDto?> GetDashboardAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientProfileDto?> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(Guid patientId, PatientProfileUpdateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientAppointmentListItemDto>> GetUpcomingAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientAppointmentListItemDto>> GetAppointmentHistoryAsync(Guid patientId, int take = 50, CancellationToken cancellationToken = default);
    Task<PatientAppointmentListItemDto?> GetAppointmentAsync(Guid patientId, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<bool> ConfirmAppointmentAsync(Guid patientId, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<bool> CancelAppointmentAsync(Guid patientId, Guid appointmentId, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientInvoiceListItemDto>> GetInvoicesAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientInvoiceListItemDto?> GetInvoiceAsync(Guid patientId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientPaymentListItemDto>> GetPaymentsAsync(Guid patientId, int take = 50, CancellationToken cancellationToken = default);
    Task<(decimal BalanceDue, decimal TotalPaid)> GetAccountStatusAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientNotificationItemDto>> GetNotificationsAsync(Guid patientId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetUnreadNotificationsCountAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task MarkNotificationReadAsync(Guid patientId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<Guid?> GetPatientIdByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalDoctorDto>> GetAvailableDoctorsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> RequestAppointmentAsync(Guid patientId, PortalAppointmentRequestDto dto, CancellationToken cancellationToken = default);
}
