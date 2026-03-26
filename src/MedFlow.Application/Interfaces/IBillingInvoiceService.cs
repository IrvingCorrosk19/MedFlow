using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IBillingInvoiceService
{
    Task<BillingInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BillingInvoice>> SearchAsync(Guid? patientId, DateTime? from, DateTime? to, MedFlow.Domain.Enums.InvoiceStatus? status, CancellationToken cancellationToken = default);
    Task<string> GenerateNextInvoiceNumberAsync(CancellationToken cancellationToken = default);
    Task<(BillingInvoice Invoice, string? Error)> CreateAsync(BillingInvoice invoice, IReadOnlyList<BillingInvoiceItem> items, CancellationToken cancellationToken = default);
    Task<(BillingInvoice Invoice, string? Error)> UpdateAsync(BillingInvoice invoice, IReadOnlyList<BillingInvoiceItem> items, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
