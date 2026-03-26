using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedFlow.Web.Areas.PatientPortal.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PatientPortalAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", new { area = "PatientPortal", returnUrl = context.HttpContext.Request.Path });
            return;
        }

        if (!context.HttpContext.User.IsInRole("Patient"))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "PatientPortal" });
            return;
        }

        var patientIdClaim = context.HttpContext.User.FindFirst("patient_id")?.Value;
        if (string.IsNullOrEmpty(patientIdClaim) || !Guid.TryParse(patientIdClaim, out _))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "PatientPortal" });
            return;
        }

        await Task.CompletedTask;
    }
}
