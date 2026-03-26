using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class SaaSInvoice : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid? TenantSubscriptionId { get; set; }
    public TenantSubscription? TenantSubscription { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";

    public SaaSInvoiceStatus Status { get; set; }

    public string? ProviderInvoiceId { get; set; }
    public string? InvoiceUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? Notes { get; set; }
}
