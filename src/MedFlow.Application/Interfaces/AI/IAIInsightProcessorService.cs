namespace MedFlow.Application.Interfaces.AI;

public interface IAIInsightProcessorService
{
    Task ProcessTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
