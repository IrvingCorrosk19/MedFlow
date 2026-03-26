namespace MedFlow.Application.Interfaces;

public interface IPlanFeatureService
{
    Task<bool> HasBillingModuleAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasAutomationModuleAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasReportsModuleAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasPatientPortalAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasMultiBranchAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasAdvancedAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
