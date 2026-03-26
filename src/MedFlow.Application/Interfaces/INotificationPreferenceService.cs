using MedFlow.Application.Notifications;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface INotificationPreferenceService
{
    Task<IReadOnlyList<NotificationPreferenceDto>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task SetAsync(Guid tenantId, NotificationPreferenceEditDto dto, CancellationToken cancellationToken = default);
}
