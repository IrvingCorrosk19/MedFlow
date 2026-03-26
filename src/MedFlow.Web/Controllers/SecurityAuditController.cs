using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.AuditView)]
public class SecurityAuditController : Controller
{
    private readonly IAuditLogService _audit;

    public SecurityAuditController(IAuditLogService audit)
    {
        _audit = audit;
    }

    public async Task<IActionResult> Index(
        DateTime? from,
        DateTime? to,
        string? userId,
        string? module,
        string? action,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Auditoría";
        ViewData["PageSubtitle"] = "Registro de acciones relevantes";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Auditoría</li>";

        DateTime? fromUtc = from.HasValue ? DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc) : null;
        DateTime? toUtc = to.HasValue ? DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc) : null;

        take = Math.Clamp(take, 50, 1000);

        var logs = await _audit.SearchAsync(fromUtc, toUtc, string.IsNullOrWhiteSpace(userId) ? null : userId,
            string.IsNullOrWhiteSpace(module) ? null : module,
            string.IsNullOrWhiteSpace(action) ? null : action,
            take, cancellationToken);

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.UserId = userId;
        ViewBag.Module = module;
        ViewBag.Action = action;
        ViewBag.Take = take;

        return View(logs);
    }
}
