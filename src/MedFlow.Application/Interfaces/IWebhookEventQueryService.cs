namespace MedFlow.Application.Interfaces;

public interface IWebhookEventQueryService
{
    Task<IReadOnlyList<WebhookEventVm>> GetRecentStripeWebhooksAsync(int count = 50, bool failedOnly = false, CancellationToken cancellationToken = default);
}

public record WebhookEventVm(string ProviderEventId, string EventType, DateTime? ProcessedAt, bool IsProcessed, string? ErrorMessage, DateTime CreatedAt);
