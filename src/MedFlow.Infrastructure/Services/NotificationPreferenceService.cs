using MedFlow.Application.Interfaces;
using MedFlow.Application.Notifications;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly ApplicationDbContext _db;

    public NotificationPreferenceService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.NotificationPreferences.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderBy(p => p.EventType).ThenBy(p => p.Channel)
            .Select(p => new NotificationPreferenceDto(
                p.Id, p.EventType, p.Channel, p.IsEnabled, p.TemplateId, p.OverrideRecipient, p.OverrideWebhookUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task SetAsync(Guid tenantId, NotificationPreferenceEditDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _db.NotificationPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.EventType == dto.EventType && p.Channel == dto.Channel, cancellationToken);

        if (existing != null)
        {
            existing.IsEnabled = dto.IsEnabled;
            existing.TemplateId = dto.TemplateId;
            existing.OverrideRecipient = dto.OverrideRecipient;
            existing.OverrideWebhookUrl = dto.OverrideWebhookUrl;
        }
        else
        {
            _db.NotificationPreferences.Add(new NotificationPreference
            {
                TenantId = tenantId,
                EventType = dto.EventType,
                Channel = dto.Channel,
                IsEnabled = dto.IsEnabled,
                TemplateId = dto.TemplateId,
                OverrideRecipient = dto.OverrideRecipient,
                OverrideWebhookUrl = dto.OverrideWebhookUrl
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
