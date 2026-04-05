using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

/// <summary>
/// Resolves the subscription plan the same way commercial middleware does:
/// prefer <see cref="Tenant.CurrentSubscription"/> when it is active, otherwise
/// the newest eligible subscription by <see cref="TenantSubscription.StartDate"/>.
/// </summary>
internal static class TenantSubscriptionPlanHelper
{
    private static readonly SubscriptionStatus[] EligibleStatuses =
    {
        SubscriptionStatus.Trial,
        SubscriptionStatus.Active,
        SubscriptionStatus.PastDue
    };

    public static async Task<SubscriptionPlan?> GetEffectivePlanAsync(
        ApplicationDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(t => t.CurrentSubscription!)
            .ThenInclude(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        var current = tenant?.CurrentSubscription;
        if (current != null && !current.IsDeleted && EligibleStatuses.Contains(current.Status))
            return current.SubscriptionPlan;

        var sub = await db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => EligibleStatuses.Contains(s.Status))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return sub?.SubscriptionPlan;
    }
}
