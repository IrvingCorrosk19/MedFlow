using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class SaaSBillingTransaction : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid? TenantSubscriptionId { get; set; }
    public TenantSubscription? TenantSubscription { get; set; }

    public SaasTransactionType TransactionType { get; set; }
    public SaasTransactionStatus Status { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public string? ProviderTransactionId { get; set; }
    public string? ProviderInvoiceId { get; set; }
    public string? ProviderPaymentIntentId { get; set; }

    public string? Description { get; set; }
    public string? FailureReason { get; set; }

    public DateTime OccurredAt { get; set; }
}
