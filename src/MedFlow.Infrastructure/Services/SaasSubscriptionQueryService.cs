using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class SaasSubscriptionQueryService : ISaasSubscriptionQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly ISubscriptionLimitService _limits;

    public SaasSubscriptionQueryService(ApplicationDbContext db, ISubscriptionLimitService limits)
    {
        _db = db;
        _limits = limits;
    }

    public async Task<IReadOnlyList<SaasSubscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var subs = await _db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.Tenant)
            .Include(s => s.SubscriptionPlan)
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);

        return subs.Select(s => new SaasSubscriptionListItemDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = s.Tenant.Name,
            TenantCode = s.Tenant.Code,
            PlanName = s.SubscriptionPlan.Name,
            Status = s.Status,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            TrialEndDate = s.TrialEndDate,
            NextBillingDate = s.NextBillingDate
        }).ToList();
    }

    public async Task<SaasSubscriptionDetailsDto?> GetDetailsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var s = await _db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.Tenant)
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && !x.IsDeleted, cancellationToken);
        if (s == null) return null;

        var usage = await _limits.GetCurrentUsageAsync(s.TenantId, cancellationToken);

        return new SaasSubscriptionDetailsDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = s.Tenant.Name,
            TenantCode = s.Tenant.Code,
            SubscriptionPlanId = s.SubscriptionPlanId,
            PlanName = s.SubscriptionPlan.Name,
            PlanCode = s.SubscriptionPlan.Code,
            Status = s.Status,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            TrialStartDate = s.TrialStartDate,
            TrialEndDate = s.TrialEndDate,
            NextBillingDate = s.NextBillingDate,
            CancelledAt = s.CancelledAt,
            SuspendedAt = s.SuspendedAt,
            Notes = s.Notes,
            ExternalSubscriptionId = s.ExternalSubscriptionId,
            ExternalPlanId = s.ExternalPlanId,
            Usage = usage
        };
    }
}
