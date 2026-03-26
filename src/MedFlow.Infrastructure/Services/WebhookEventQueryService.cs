using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class WebhookEventQueryService : IWebhookEventQueryService
{
    private readonly ApplicationDbContext _db;

    public WebhookEventQueryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WebhookEventVm>> GetRecentStripeWebhooksAsync(int count = 50, bool failedOnly = false, CancellationToken cancellationToken = default)
    {
        var q = _db.StripeWebhookEventLogs
            .IgnoreQueryFilters()
            .OrderByDescending(e => e.CreatedAt)
            .Take(count);

        if (failedOnly)
            q = q.Where(e => !e.IsProcessed || e.ErrorMessage != null);

        var list = await q
            .Select(e => new WebhookEventVm(
                e.ProviderEventId,
                e.EventType,
                e.ProcessedAt,
                e.IsProcessed,
                e.ErrorMessage,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return list;
    }
}
