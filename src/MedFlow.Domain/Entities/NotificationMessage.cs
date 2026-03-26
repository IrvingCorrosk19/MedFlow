using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class NotificationMessage : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationMessageStatus Status { get; set; } = NotificationMessageStatus.Pending;

    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }

    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public DateTime? SentAt { get; set; }

    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }

    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }

    public string? WebhookPayload { get; set; }
    public string? WebhookResponse { get; set; }
}
