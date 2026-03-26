namespace MedFlow.Application.Interfaces;

public interface IStripeWebhookProcessor
{
    Task ProcessAsync(string jsonPayload, string signatureHeader, CancellationToken cancellationToken = default);
}
