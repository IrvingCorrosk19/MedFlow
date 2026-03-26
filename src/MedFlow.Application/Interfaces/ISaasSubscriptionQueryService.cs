using MedFlow.Application.Saas;

namespace MedFlow.Application.Interfaces;

public interface ISaasSubscriptionQueryService
{
    Task<IReadOnlyList<SaasSubscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SaasSubscriptionDetailsDto?> GetDetailsAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
