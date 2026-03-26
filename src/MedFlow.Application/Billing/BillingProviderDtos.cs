using MedFlow.Domain.Enums;

namespace MedFlow.Application.Billing;

public sealed record CreateCustomerRequest(
    string Email,
    string? Name,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public sealed record CreateCustomerResult(string ExternalCustomerId);

public sealed record CreateCheckoutSessionRequest(
    string ExternalCustomerId,
    string PriceId,
    Guid TenantId,
    Guid SubscriptionPlanId,
    BillingPeriod BillingPeriod,
    string SuccessUrl,
    string CancelUrl,
    string? CouponId = null);

public sealed record CreateCheckoutSessionResult(string SessionId, string SessionUrl);

public sealed record CreateSubscriptionRequest(
    string ExternalCustomerId,
    string PriceId,
    Guid TenantId,
    Guid SubscriptionPlanId,
    BillingPeriod BillingPeriod,
    int? TrialDays = null);

public sealed record CreateSubscriptionResult(
    string ExternalSubscriptionId,
    string ExternalPriceId,
    string? ExternalProductId,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    string? Status);

public sealed record ChangeSubscriptionPlanRequest(
    string ExternalSubscriptionId,
    string NewPriceId,
    bool Prorate = true);

public sealed record CancelSubscriptionRequest(
    string ExternalSubscriptionId,
    bool CancelAtPeriodEnd = true);

public sealed record ResumeSubscriptionRequest(string ExternalSubscriptionId);
