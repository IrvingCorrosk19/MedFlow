namespace MedFlow.Application.Interfaces;

public interface ITenantHealthService
{
    Task<TenantHealthVm> GetHealthScoreAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record TenantHealthVm(
    decimal Score,
    string Classification,
    string Summary,
    IReadOnlyList<string> Factors,
    IReadOnlyList<string> Recommendations);
