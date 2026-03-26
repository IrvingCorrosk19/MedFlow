using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Notifications;

public sealed class NotificationDispatchService : INotificationDispatchService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IWebhookSender _webhook;

    public NotificationDispatchService(ApplicationDbContext db, IEmailSender email, IWebhookSender webhook)
    {
        _db = db;
        _email = email;
        _webhook = webhook;
    }

    public async Task<DispatchResult> DispatchAsync(DispatchRequest request, CancellationToken cancellationToken = default)
    {
        var messageIds = new List<Guid>();
        var errors = new List<string>();

        var prefs = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .Include(p => p.Template)
            .Where(p => p.TenantId == request.TenantId && !p.IsDeleted && p.IsEnabled && p.EventType == request.EventType)
            .ToListAsync(cancellationToken);

        if (prefs.Count == 0)
            return new DispatchResult(true, [], []);

        foreach (var pref in prefs)
        {
            var template = pref.Template ?? await GetDefaultTemplateAsync(request.TenantId, request.EventType, pref.Channel, cancellationToken);
            if (template == null)
            {
                errors.Add($"No template for {request.EventType} / {pref.Channel}");
                continue;
            }

            var (subject, body, htmlBody) = RenderTemplate(template, request.Payload);
            var recipient = GetRecipient(request, pref, pref.Channel);

            if (string.IsNullOrWhiteSpace(recipient) && pref.Channel != NotificationChannel.Webhook)
            {
                errors.Add($"No recipient for {pref.Channel}");
                continue;
            }

            var msg = new NotificationMessage
            {
                TenantId = request.TenantId,
                EventType = request.EventType,
                Channel = pref.Channel,
                Status = NotificationMessageStatus.Pending,
                Recipient = recipient,
                Subject = subject,
                Body = body ?? htmlBody,
                TemplateId = template.Id,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId
            };
            _db.NotificationMessages.Add(msg);
            await _db.SaveChangesAsync(cancellationToken);
            messageIds.Add(msg.Id);

            try
            {
                switch (pref.Channel)
                {
                    case NotificationChannel.Email:
                        await DispatchEmailAsync(msg, recipient!, subject, htmlBody, body, template, cancellationToken);
                        break;
                    case NotificationChannel.Webhook:
                        await DispatchWebhookAsync(msg, pref, template, request, cancellationToken);
                        break;
                    case NotificationChannel.InApp:
                        await DispatchInAppAsync(msg, request, cancellationToken);
                        break;
                    case NotificationChannel.WhatsApp:
                        msg.Status = NotificationMessageStatus.Pending;
                        msg.ErrorMessage = "WhatsApp channel not yet implemented";
                        break;
                    default:
                        msg.Status = NotificationMessageStatus.Failed;
                        msg.ErrorMessage = $"Channel {pref.Channel} not supported";
                        break;
                }
            }
            catch (Exception ex)
            {
                msg.Status = NotificationMessageStatus.Failed;
                msg.ErrorMessage = ex.Message;
                msg.RetryCount++;
                errors.Add(ex.Message);
            }

            if (msg.Status == NotificationMessageStatus.Sent)
                msg.SentAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        return new DispatchResult(errors.Count == 0, messageIds, errors);
    }

    private async Task DispatchEmailAsync(NotificationMessage msg, string to, string? subject, string? htmlBody, string? textBody, NotificationTemplate template, CancellationToken ct)
    {
        var result = await _email.SendAsync(to, subject, htmlBody, textBody, template.FromEmail, template.FromName, template.ReplyTo, ct);
        msg.ExternalId = result.ExternalId;
        msg.Status = result.Success ? NotificationMessageStatus.Sent : NotificationMessageStatus.Failed;
        msg.ErrorMessage = result.ErrorMessage;
    }

    private async Task DispatchWebhookAsync(NotificationMessage msg, NotificationPreference pref, NotificationTemplate template, DispatchRequest request, CancellationToken ct)
    {
        var url = pref.OverrideWebhookUrl ?? template.WebhookUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            msg.Status = NotificationMessageStatus.Failed;
            msg.ErrorMessage = "No webhook URL configured";
            return;
        }

        var payload = new Dictionary<string, object>(request.Payload)
        {
            ["EventType"] = request.EventType.ToString(),
            ["TenantId"] = request.TenantId.ToString(),
            ["OccurredAt"] = DateTime.UtcNow
        };
        if (request.RelatedEntityType != null) payload["RelatedEntityType"] = request.RelatedEntityType;
        if (request.RelatedEntityId != null) payload["RelatedEntityId"] = request.RelatedEntityId;

        msg.WebhookPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var result = await _webhook.SendAsync(url, payload, template.WebhookMethod ?? "POST", ct);
        msg.WebhookResponse = result.ResponseBody;
        msg.Status = result.Success ? NotificationMessageStatus.Sent : NotificationMessageStatus.Failed;
        msg.ErrorMessage = result.ErrorMessage;
    }

    private async Task DispatchInAppAsync(NotificationMessage msg, DispatchRequest request, CancellationToken ct)
    {
        var notification = new Notification
        {
            TenantId = request.TenantId,
            UserId = request.RecipientUserId,
            PatientId = request.RelatedEntityId,
            Title = msg.Subject ?? request.EventType.ToString(),
            Message = msg.Body,
            Type = "Transactional",
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId
        };
        _db.Notifications.Add(notification);
        msg.ExternalId = notification.Id.ToString();
        msg.Status = NotificationMessageStatus.Sent;
        await _db.SaveChangesAsync(ct);
    }

    private static string? GetRecipient(DispatchRequest request, NotificationPreference pref, NotificationChannel channel)
    {
        var over = pref.OverrideRecipient;
        if (!string.IsNullOrWhiteSpace(over)) return over;
        return channel switch
        {
            NotificationChannel.Email => request.RecipientEmail,
            NotificationChannel.InApp => request.RecipientUserId,
            NotificationChannel.WhatsApp => request.RecipientPhone,
            _ => null
        };
    }

    private async Task<NotificationTemplate?> GetDefaultTemplateAsync(Guid tenantId, NotificationEventType eventType, NotificationChannel channel, CancellationToken ct)
    {
        return await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.EventType == eventType && t.Channel == channel && (t.IsDefault || true), ct);
    }

    private static (string? subject, string? body, string? htmlBody) RenderTemplate(NotificationTemplate t, IReadOnlyDictionary<string, object> payload)
    {
        string Replace(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            foreach (var kv in payload)
                s = s.Replace("{{" + kv.Key + "}}", kv.Value?.ToString() ?? "");
            return s;
        }

        return (Replace(t.SubjectTemplate), Replace(t.BodyTemplate), Replace(t.HtmlBodyTemplate));
    }
}
