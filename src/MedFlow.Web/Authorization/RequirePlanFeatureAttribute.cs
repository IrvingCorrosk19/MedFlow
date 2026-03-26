using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace MedFlow.Web.Authorization;

public enum PlanFeatureKind
{
    Billing,
    Automation,
    Reports,
    PatientPortal,
    MultiBranch,
    AdvancedAnalytics
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePlanFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    public RequirePlanFeatureAttribute(PlanFeatureKind feature) => Feature = feature;

    public PlanFeatureKind Feature { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (http.User?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userManager = http.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var tc = http.RequestServices.GetRequiredService<ITenantContext>();
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var prevIgnore = tc.IgnoreTenantFilter;
            tc.SetIgnoreTenantFilter(true);
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null && await userManager.IsInRoleAsync(user, "SuperAdmin"))
                    return;
            }
            finally
            {
                tc.SetIgnoreTenantFilter(prevIgnore);
            }
        }
        if (!tc.TenantId.HasValue)
            return;

        var svc = http.RequestServices.GetRequiredService<IPlanFeatureService>();
        var tid = tc.TenantId.Value;
        var ct = http.RequestAborted;

        var ok = Feature switch
        {
            PlanFeatureKind.Billing => await svc.HasBillingModuleAsync(tid, ct),
            PlanFeatureKind.Automation => await svc.HasAutomationModuleAsync(tid, ct),
            PlanFeatureKind.Reports => await svc.HasReportsModuleAsync(tid, ct),
            PlanFeatureKind.PatientPortal => await svc.HasPatientPortalAsync(tid, ct),
            PlanFeatureKind.MultiBranch => await svc.HasMultiBranchAsync(tid, ct),
            PlanFeatureKind.AdvancedAnalytics => await svc.HasAdvancedAnalyticsAsync(tid, ct),
            _ => false
        };

        if (!ok)
            context.Result = new RedirectToActionResult("UpgradeRequired", "Commercial", new { feature = Feature.ToString() });
    }
}
