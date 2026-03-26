using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Tenancy;

public sealed class TenantCommercialMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SaasOptions _saas;

    public TenantCommercialMiddleware(RequestDelegate next, IOptions<SaasOptions> saasOptions)
    {
        _next = next;
        _saas = saasOptions.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Onboarding", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Commercial", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var prevIgnore = tenantContext.IgnoreTenantFilter;
            tenantContext.SetIgnoreTenantFilter(true);
            var user = await userManager.FindByIdAsync(userId);
            if (user != null && await userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                tenantContext.SetIgnoreTenantFilter(true);
                await _next(context);
                return;
            }

            tenantContext.SetIgnoreTenantFilter(prevIgnore);
        }

        if (!tenantContext.TenantId.HasValue)
        {
            await _next(context);
            return;
        }

        var tenantId = tenantContext.TenantId.Value;

        var tenant = await db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(t => t.CurrentSubscription!)
                .ThenInclude(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, context.RequestAborted);

        if (tenant == null)
        {
            await _next(context);
            return;
        }

        var sub = tenant.CurrentSubscription;
        var now = DateTime.UtcNow;

        bool suspendedOperationally = tenant.IsSuspended ||
            (sub != null && sub.Status == SubscriptionStatus.Suspended);

        if (suspendedOperationally)
        {
            if (!IsPathAllowedWhenSuspended(path))
            {
                context.Response.Redirect("/Commercial/Blocked?reason=suspended");
                return;
            }

            SetPlanFeatureItems(context, sub?.SubscriptionPlan);
            context.Items["TenantCommercialSuspendedLimited"] = true;
            await _next(context);
            return;
        }

        if (sub == null)
        {
            context.Response.Redirect("/Commercial/Blocked?reason=no_subscription");
            return;
        }

        if (sub.Status == SubscriptionStatus.Trial && sub.TrialEndDate.HasValue && now > sub.TrialEndDate.Value)
        {
            context.Response.Redirect("/Commercial/Blocked?reason=trial_expired");
            return;
        }

        if (sub.Status == SubscriptionStatus.Cancelled || sub.Status == SubscriptionStatus.Expired)
        {
            context.Response.Redirect("/Commercial/Blocked?reason=subscription_inactive");
            return;
        }

        if (sub.EndDate.HasValue && now > sub.EndDate.Value && sub.Status != SubscriptionStatus.Trial)
        {
            context.Response.Redirect("/Commercial/Blocked?reason=subscription_expired");
            return;
        }

        if (sub.Status == SubscriptionStatus.PastDue)
        {
            if (!_saas.AllowOperationsWhenPastDue)
            {
                context.Response.Redirect("/Commercial/Blocked?reason=past_due_locked");
                return;
            }

            context.Items["TenantCommercialPastDue"] = true;
        }

        SetPlanFeatureItems(context, sub.SubscriptionPlan);
        await _next(context);
    }

    private bool IsPathAllowedWhenSuspended(string path)
    {
        foreach (var prefix in _saas.SuspendedTenantAllowedPathPrefixes)
        {
            if (string.IsNullOrEmpty(prefix)) continue;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void SetPlanFeatureItems(HttpContext context, SubscriptionPlan? plan)
    {
        if (plan == null)
        {
            context.Items["PlanFeatureBilling"] = false;
            context.Items["PlanFeatureAutomation"] = false;
            context.Items["PlanFeatureReports"] = false;
            context.Items["PlanFeaturePatientPortal"] = false;
            context.Items["PlanFeatureMultiBranch"] = false;
            context.Items["PlanFeatureAdvancedAnalytics"] = false;
            return;
        }

        context.Items["PlanFeatureBilling"] = plan.IncludesBillingModule;
        context.Items["PlanFeatureAutomation"] = plan.IncludesAutomationModule;
        context.Items["PlanFeatureReports"] = plan.IncludesReportsModule;
        context.Items["PlanFeaturePatientPortal"] = plan.IncludesPatientPortal;
        context.Items["PlanFeatureMultiBranch"] = plan.IncludesMultiBranch;
        context.Items["PlanFeatureAdvancedAnalytics"] = plan.IncludesAdvancedAnalytics;
    }
}
