using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly ApplicationDbContext _db;

    public NotificationTemplateService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.NotificationTemplates.IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted)
            .OrderBy(t => t.EventType).ThenBy(t => t.Channel)
            .Select(t => ToDto(t))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        return t == null ? null : ToDto(t);
    }

    public async Task<NotificationTemplateDto?> GetByEventAndChannelAsync(Guid tenantId, NotificationEventType eventType, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var t = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.EventType == eventType && x.Channel == channel, cancellationToken);
        return t == null ? null : ToDto(t);
    }

    public async Task<Guid> CreateAsync(Guid tenantId, NotificationTemplateEditDto dto, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(dto.Code);
        var exists = await _db.NotificationTemplates.IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId && !t.IsDeleted && t.Code == code, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Ya existe una plantilla con ese código.");

        var t = new NotificationTemplate
        {
            TenantId = tenantId,
            EventType = dto.EventType,
            Channel = dto.Channel,
            Code = code,
            Name = dto.Name.Trim(),
            SubjectTemplate = dto.SubjectTemplate,
            BodyTemplate = dto.BodyTemplate,
            HtmlBodyTemplate = dto.HtmlBodyTemplate,
            FromEmail = dto.FromEmail,
            FromName = dto.FromName,
            ReplyTo = dto.ReplyTo,
            WebhookUrl = dto.WebhookUrl,
            WebhookMethod = dto.WebhookMethod ?? "POST",
            ResendTemplateId = dto.ResendTemplateId,
            WhatsAppTemplateId = dto.WhatsAppTemplateId,
            IsDefault = dto.IsDefault,
            Description = dto.Description
        };
        _db.NotificationTemplates.Add(t);
        await _db.SaveChangesAsync(cancellationToken);
        return t.Id;
    }

    public async Task UpdateAsync(Guid id, NotificationTemplateEditDto dto, CancellationToken cancellationToken = default)
    {
        var t = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (t == null) throw new InvalidOperationException("Plantilla no encontrada.");

        var code = NormalizeCode(dto.Code);
        var exists = await _db.NotificationTemplates.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == t.TenantId && !x.IsDeleted && x.Code == code && x.Id != id, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Ya existe otra plantilla con ese código.");

        t.Code = code;
        t.Name = dto.Name.Trim();
        t.EventType = dto.EventType;
        t.Channel = dto.Channel;
        t.SubjectTemplate = dto.SubjectTemplate;
        t.BodyTemplate = dto.BodyTemplate;
        t.HtmlBodyTemplate = dto.HtmlBodyTemplate;
        t.FromEmail = dto.FromEmail;
        t.FromName = dto.FromName;
        t.ReplyTo = dto.ReplyTo;
        t.WebhookUrl = dto.WebhookUrl;
        t.WebhookMethod = dto.WebhookMethod ?? "POST";
        t.ResendTemplateId = dto.ResendTemplateId;
        t.WhatsAppTemplateId = dto.WhatsAppTemplateId;
        t.IsDefault = dto.IsDefault;
        t.Description = dto.Description;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (t == null) throw new InvalidOperationException("Plantilla no encontrada.");

        t.IsDeleted = true;
        t.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static NotificationTemplateDto ToDto(NotificationTemplate t) => new(
        t.Id, t.TenantId, t.EventType, t.Channel, t.Code, t.Name,
        t.SubjectTemplate, t.BodyTemplate, t.HtmlBodyTemplate,
        t.FromEmail, t.FromName, t.ReplyTo, t.WebhookUrl, t.WebhookMethod ?? "POST", t.ResendTemplateId, t.WhatsAppTemplateId,
        t.IsDefault, t.Description);

    private static string NormalizeCode(string code) => code.Trim().ToLowerInvariant().Replace(" ", "-");
}
