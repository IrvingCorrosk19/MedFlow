using MedFlow.Application.Billing;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Enums;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace MedFlow.Infrastructure.Billing;

public sealed class StripeBillingProvider : IBillingProvider
{
    private readonly StripeBillingOptions _options;

    public StripeBillingProvider(IOptions<StripeBillingOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrEmpty(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<CreateCustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = request.Email,
            Name = request.Name,
            Phone = request.Phone,
            Address = new AddressOptions
            {
                Line1 = request.AddressLine1,
                Line2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country
            }
        }, cancellationToken: cancellationToken);
        return new CreateCustomerResult(customer.Id);
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionService = new SessionService();
        var options = new SessionCreateOptions
        {
            Customer = request.ExternalCustomerId,
            Mode = "subscription",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = request.TenantId.ToString(),
                    ["plan_id"] = request.SubscriptionPlanId.ToString(),
                    ["billing_period"] = request.BillingPeriod.ToString()
                }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = request.PriceId,
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString(),
                ["plan_id"] = request.SubscriptionPlanId.ToString()
            }
        };
        if (!string.IsNullOrEmpty(request.CouponId))
            options.Discounts = [new SessionDiscountOptions { Coupon = request.CouponId }];

        var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);
        return new CreateCheckoutSessionResult(session.Id, session.Url ?? "");
    }

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var subService = new SubscriptionService();
        var options = new SubscriptionCreateOptions
        {
            Customer = request.ExternalCustomerId,
            Items = [new SubscriptionItemOptions { Price = request.PriceId }],
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString(),
                ["plan_id"] = request.SubscriptionPlanId.ToString()
            }
        };
        if (request.TrialDays.HasValue && request.TrialDays.Value > 0)
            options.TrialPeriodDays = request.TrialDays.Value;

        var sub = await subService.CreateAsync(options, cancellationToken: cancellationToken);
        var item = sub.Items?.Data?.FirstOrDefault();
        return new CreateSubscriptionResult(
            sub.Id,
            item?.Price?.Id ?? "",
            item?.Price?.ProductId,
            ToUtc(sub.CurrentPeriodStart),
            ToUtc(sub.CurrentPeriodEnd),
            sub.Status);
    }

    public async Task<CreateSubscriptionResult?> GetSubscriptionAsync(string externalSubscriptionId, CancellationToken cancellationToken = default)
    {
        var service = new SubscriptionService();
        try
        {
            var sub = await service.GetAsync(externalSubscriptionId, cancellationToken: cancellationToken);
            var item = sub.Items?.Data?.FirstOrDefault();
            return new CreateSubscriptionResult(
                sub.Id,
                item?.Price?.Id ?? "",
                item?.Price?.ProductId,
                ToUtc(sub.CurrentPeriodStart),
                ToUtc(sub.CurrentPeriodEnd),
                sub.Status);
        }
        catch (StripeException)
        {
            return null;
        }
    }

    public async Task ChangePlanAsync(ChangeSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(request.ExternalSubscriptionId, cancellationToken: cancellationToken);
        var itemId = sub.Items?.Data?.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(itemId))
            throw new InvalidOperationException("Subscription has no items.");

        await subService.UpdateAsync(request.ExternalSubscriptionId, new SubscriptionUpdateOptions
        {
            Items = [new SubscriptionItemOptions { Id = itemId, Price = request.NewPriceId }],
            ProrationBehavior = request.Prorate ? "create_prorations" : "none"
        }, cancellationToken: cancellationToken);
    }

    public async Task CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var subService = new SubscriptionService();
        if (request.CancelAtPeriodEnd)
        {
            await subService.UpdateAsync(request.ExternalSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            }, cancellationToken: cancellationToken);
        }
        else
        {
            await subService.CancelAsync(request.ExternalSubscriptionId, cancellationToken: cancellationToken);
        }
    }

    public async Task ResumeSubscriptionAsync(ResumeSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var subService = new SubscriptionService();
        await subService.UpdateAsync(request.ExternalSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = false
        }, cancellationToken: cancellationToken);
    }

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();

    public bool ValidateWebhookSignature(string payload, string signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(secret))
            return false;
        try
        {
            EventUtility.ConstructEvent(payload, signatureHeader, secret);
            return true;
        }
        catch (StripeException)
        {
            return false;
        }
    }
}
