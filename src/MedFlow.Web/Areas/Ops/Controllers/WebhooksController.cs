using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.Ops.Controllers;

[Area("Ops")]
[Route("Ops/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class WebhooksController : Controller
{
    private readonly IWebhookEventQueryService _webhooks;

    public WebhooksController(IWebhookEventQueryService webhooks)
    {
        _webhooks = webhooks;
    }

    [Route("")]
    [Route("Index")]
    [HttpGet]
    public async Task<IActionResult> Index(bool failedOnly = false, CancellationToken ct = default)
    {
        var events = await _webhooks.GetRecentStripeWebhooksAsync(100, failedOnly, ct);
        ViewData["Title"] = "Webhooks";
        ViewData["FailedOnly"] = failedOnly;
        return View(events);
    }
}
