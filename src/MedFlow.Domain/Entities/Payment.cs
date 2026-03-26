using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class Payment : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid BillingInvoiceId { get; set; }
    public BillingInvoice BillingInvoice { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public ClinicalPaymentMethod PaymentMethod { get; set; } = ClinicalPaymentMethod.Cash;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReceivedByUserId { get; set; }
    public PaymentEntryStatus Status { get; set; } = PaymentEntryStatus.Registered;

    public ICollection<CashMovement> CashMovements { get; set; } = [];
}
