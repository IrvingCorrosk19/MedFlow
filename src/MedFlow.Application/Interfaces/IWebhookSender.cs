namespace MedFlow.Application.Interfaces;

public interface IWebhookSender
{
    Task<WebhookSendResult> SendAsync(string url, object payload, string method = "POST", CancellationToken cancellationToken = default);
}

public sealed record WebhookSendResult(bool Success, string? ResponseBody, string? ErrorMessage);
