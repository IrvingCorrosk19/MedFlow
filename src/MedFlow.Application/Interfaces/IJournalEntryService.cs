using MedFlow.Application.Accounting;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface IJournalEntryService
{
    Task<IReadOnlyList<JournalEntryListItemDto>> SearchAsync(
        Guid tenantId,
        DateTime? from, DateTime? to,
        JournalEntryStatus? status,
        JournalEntryOrigin? origin,
        string? reference,
        CancellationToken ct = default);

    Task<JournalEntryDetailDto?> GetDetailAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<JournalEntry?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<(JournalEntry Entry, string? Error)> CreateAsync(
        Guid tenantId,
        JournalEntryFormDto form,
        string userId,
        CancellationToken ct = default);

    Task<(bool Ok, string? Error)> UpdateAsync(
        Guid id,
        JournalEntryFormDto form,
        CancellationToken ct = default);

    Task<(bool Ok, string? Error)> PostAsync(Guid id, string userId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> VoidAsync(Guid id, string userId, string reason, CancellationToken ct = default);

    /// <summary>Crea automáticamente un asiento al emitir una factura.</summary>
    Task<JournalEntry?> CreateFromInvoiceAsync(Guid tenantId, BillingInvoice invoice, string userId, CancellationToken ct = default);

    /// <summary>Crea automáticamente un asiento al registrar un pago.</summary>
    Task<JournalEntry?> CreateFromPaymentAsync(Guid tenantId, Payment payment, string userId, CancellationToken ct = default);

    Task<string> GenerateNextEntryNumberAsync(Guid tenantId, CancellationToken ct = default);
}
