using MedFlow.Application.Billing;

namespace MedFlow.Application.Interfaces;

public interface IBillingProvider
{
    Task<CreateCustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default);
    Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<CreateSubscriptionResult?> GetSubscriptionAsync(string externalSubscriptionId, CancellationToken cancellationToken = default);
    Task ChangePlanAsync(ChangeSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task ResumeSubscriptionAsync(ResumeSubscriptionRequest request, CancellationToken cancellationToken = default);
    bool ValidateWebhookSignature(string payload, string signatureHeader, string secret);
}
