using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface IPaymentService
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> SearchAsync(Guid? invoiceId, Guid? patientId, DateTime? from, DateTime? to, ClinicalPaymentMethod? method, CancellationToken cancellationToken = default);
    Task<(Payment? Payment, string? Error)> RegisterAsync(Guid billingInvoiceId, Guid patientId, DateTime paymentDate, decimal amount, ClinicalPaymentMethod method, string? referenceNumber, string? notes, string? receivedByUserId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CancelPaymentAsync(Guid paymentId, string? actorUserId, CancellationToken cancellationToken = default);
}
