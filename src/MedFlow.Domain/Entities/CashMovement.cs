using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class CashMovement : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTime MovementDate { get; set; }
    public CashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    public Guid? RelatedPaymentId { get; set; }
    public Payment? RelatedPayment { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
}
