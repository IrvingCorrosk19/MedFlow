using MedFlow.Application.Notifications;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface INotificationTemplateService
{
    Task<IReadOnlyList<NotificationTemplateDto>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto?> GetByEventAndChannelAsync(Guid tenantId, NotificationEventType eventType, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Guid tenantId, NotificationTemplateEditDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, NotificationTemplateEditDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
