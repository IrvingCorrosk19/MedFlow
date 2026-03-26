using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        return plan != null && predicate(plan);
    }

    private async Task<SubscriptionPlan?> GetPlanAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
