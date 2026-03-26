using MedFlow.Application.Saas;

namespace MedFlow.Application.Interfaces;

public interface ISubscriptionPlanAdminService
{
    Task<IReadOnlyList<SubscriptionPlanListDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SubscriptionPlanEditDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SubscriptionPlanEditDto dto, CancellationToken cancellationToken = default);
}
