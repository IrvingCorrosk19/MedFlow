using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class BillingInvoiceItem : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid BillingInvoiceId { get; set; }
    public BillingInvoice BillingInvoice { get; set; } = null!;

    public BillingInvoiceItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalLineAmount { get; set; }
}
