using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class NotificationTemplate : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string? SubjectTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public string? HtmlBodyTemplate { get; set; }

    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }

    public string? WebhookUrl { get; set; }
    public string? WebhookMethod { get; set; } = "POST";

    public string? ResendTemplateId { get; set; }
    public string? WhatsAppTemplateId { get; set; }

    public bool IsDefault { get; set; }
    public string? Description { get; set; }
}
