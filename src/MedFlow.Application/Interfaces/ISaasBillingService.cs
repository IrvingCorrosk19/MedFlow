using MedFlow.Application.Billing;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface ISaasBillingService
{
    Task<TenantBillingProfileDto?> GetOrCreateBillingProfileAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(Guid tenantId, Guid planId, BillingPeriod period, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);
    Task ChangePlanAsync(Guid tenantId, Guid newPlanId, bool prorate, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(Guid tenantId, bool atPeriodEnd, CancellationToken cancellationToken = default);
    Task ResumeSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdateBillingProfileAsync(Guid tenantId, TenantBillingProfileUpdateDto dto, CancellationToken cancellationToken = default);
}

public sealed record TenantBillingProfileDto(
    Guid Id,
    Guid TenantId,
    string? BillingEmail,
    string? LegalName,
    string? TaxId,
    string? Country,
    string? AddressLine1,
    string? ExternalCustomerId,
    string? PreferredCurrency);

public sealed record TenantBillingProfileUpdateDto(
    string? BillingEmail,
    string? LegalName,
    string? TaxId,
    string? Country,
    string? StateProvince,
    string? City,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    string? PreferredCurrency);
