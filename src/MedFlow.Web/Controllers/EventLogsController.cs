using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers;

[Authorize]
[RequirePermission(PermissionCodes.EventLogsView)]
public class EventLogsController : Controller
{
    private readonly IEventLogQueryService _logs;

    public EventLogsController(IEventLogQueryService logs)
    {
        _logs = logs;
    }

    public async Task<IActionResult> Index(OutboxEventStatus? status, CancellationToken cancellationToken = default)
    {
        var list = await _logs.GetRecentAsync(500, status, cancellationToken);
        ViewBag.Status = status;
        ViewData["Title"] = "Eventos del sistema";
        ViewData["PageSubtitle"] = "Outbox / integración";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Eventos</li>";
        return View(list);
    }
}
