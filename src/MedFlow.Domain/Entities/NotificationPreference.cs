using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class NotificationPreference : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }

    public bool IsEnabled { get; set; } = true;

    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }

    public string? OverrideRecipient { get; set; }
    public string? OverrideWebhookUrl { get; set; }
    public string? Notes { get; set; }
}
