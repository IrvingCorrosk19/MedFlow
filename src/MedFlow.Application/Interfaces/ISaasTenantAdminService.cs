using MedFlow.Application.Saas;

namespace MedFlow.Application.Interfaces;

public interface ISaasTenantAdminService
{
    Task<IReadOnlyList<SaasTenantListItemDto>> GetTenantsAsync(CancellationToken cancellationToken = default);
    Task<SaasTenantDetailsDto?> GetTenantDetailsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateTenantWithSubscriptionAsync(SaasTenantCreateDto dto, string? changedByUserId, CancellationToken cancellationToken = default);
    Task SuspendTenantAsync(Guid tenantId, string reason, string? changedByUserId, CancellationToken cancellationToken = default);
    Task ActivateTenantAsync(Guid tenantId, string? changedByUserId, CancellationToken cancellationToken = default);
    Task ChangePlanAsync(Guid tenantId, Guid newPlanId, string reason, string? changedByUserId, CancellationToken cancellationToken = default);
}
