using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;

namespace MedFlow.Infrastructure.Services;

public sealed class PlanFeatureService : IPlanFeatureService
{
    private readonly ApplicationDbContext _db;

    public PlanFeatureService(ApplicationDbContext db) => _db = db;

    public Task<bool> HasBillingModuleAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesBillingModule, cancellationToken);

    public Task<bool> HasAutomationModuleAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesAutomationModule, cancellationToken);

    public Task<bool> HasReportsModuleAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesReportsModule, cancellationToken);

    public Task<bool> HasPatientPortalAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesPatientPortal, cancellationToken);

    public Task<bool> HasMultiBranchAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesMultiBranch, cancellationToken);

    public Task<bool> HasAdvancedAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        HasAsync(tenantId, p => p.IncludesAdvancedAnalytics, cancellationToken);

    private async Task<bool> HasAsync(Guid tenantId, Func<SubscriptionPlan, bool> predicate, CancellationToken cancellationToken)
    {
        var plan = await TenantSubscriptionPlanHelper.GetEffectivePlanAsync(_db, tenantId, cancellationToken);
        return plan != null && predicate(plan);
    }
}
