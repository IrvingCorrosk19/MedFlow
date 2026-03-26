using MedFlow.Domain.Enums;

namespace MedFlow.Application.Notifications;

public sealed record NotificationTemplateDto(
    Guid Id,
    Guid TenantId,
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Code,
    string Name,
    string? SubjectTemplate,
    string? BodyTemplate,
    string? HtmlBodyTemplate,
    string? FromEmail,
    string? FromName,
    string? ReplyTo,
    string? WebhookUrl,
    string? WebhookMethod,
    string? ResendTemplateId,
    string? WhatsAppTemplateId,
    bool IsDefault,
    string? Description);

public sealed record NotificationTemplateEditDto(
    NotificationEventType EventType,
    NotificationChannel Channel,
    string Code,
    string Name,
    string? SubjectTemplate,
    string? BodyTemplate,
    string? HtmlBodyTemplate,
    string? FromEmail,
    string? FromName,
    string? ReplyTo,
    string? WebhookUrl,
    string? WebhookMethod,
    string? ResendTemplateId,
    string? WhatsAppTemplateId,
    bool IsDefault,
    string? Description);

public sealed record NotificationPreferenceDto(
    Guid Id,
    NotificationEventType EventType,
    NotificationChannel Channel,
    bool IsEnabled,
    Guid? TemplateId,
    string? OverrideRecipient,
    string? OverrideWebhookUrl);

public sealed record NotificationPreferenceEditDto(
    NotificationEventType EventType,
    NotificationChannel Channel,
    bool IsEnabled,
    Guid? TemplateId,
    string? OverrideRecipient,
    string? OverrideWebhookUrl);

public sealed record NotificationMessageDto(
    Guid Id,
    NotificationEventType EventType,
    NotificationChannel Channel,
    NotificationMessageStatus Status,
    string? Recipient,
    string? Subject,
    DateTime CreatedAt,
    DateTime? SentAt,
    string? ErrorMessage);

public sealed record DispatchRequest(
    Guid TenantId,
    NotificationEventType EventType,
    IReadOnlyDictionary<string, object> Payload,
    string? RecipientEmail = null,
    string? RecipientUserId = null,
    string? RecipientPhone = null,
    string? RelatedEntityType = null,
    string? RelatedEntityId = null);
