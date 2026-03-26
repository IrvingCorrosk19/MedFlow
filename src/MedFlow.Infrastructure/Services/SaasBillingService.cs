using MedFlow.Application.Billing;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Services;

public sealed class SaasBillingService : ISaasBillingService
{
    private readonly ApplicationDbContext _db;
    private readonly IBillingProvider _provider;
    private readonly ITenantContext _tenant;
    private readonly StripeBillingOptions _stripeOptions;

    public SaasBillingService(
        ApplicationDbContext db,
        IBillingProvider provider,
        ITenantContext tenant,
        IOptions<StripeBillingOptions> stripeOptions)
    {
        _db = db;
        _provider = provider;
        _tenant = tenant;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<TenantBillingProfileDto?> GetOrCreateBillingProfileAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
            if (tenant == null) return null;

            var profile = await _db.TenantBillingProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && !b.IsDeleted, cancellationToken);

            if (profile == null)
            {
                profile = new TenantBillingProfile
                {
                    TenantId = tenantId,
                    BillingEmail = tenant.Email,
                    LegalName = tenant.Name,
                    BillingProvider = BillingProvider.Stripe,
                    PreferredCurrency = _stripeOptions.DefaultCurrency,
                    IsActive = true
                };
                if (!string.IsNullOrEmpty(tenant.Email))
                {
                    var result = await _provider.CreateCustomerAsync(new CreateCustomerRequest(
                        tenant.Email,
                        tenant.Name,
                        tenant.Phone,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null), cancellationToken);
                    profile.ExternalCustomerId = result.ExternalCustomerId;
                }
                _db.TenantBillingProfiles.Add(profile);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (string.IsNullOrEmpty(profile.ExternalCustomerId) && !string.IsNullOrEmpty(profile.BillingEmail))
            {
                var result = await _provider.CreateCustomerAsync(new CreateCustomerRequest(
                    profile.BillingEmail,
                    profile.LegalName,
                    null,
                    profile.AddressLine1,
                    profile.AddressLine2,
                    profile.City,
                    profile.StateProvince,
                    profile.PostalCode,
                    profile.Country), cancellationToken);
                profile.ExternalCustomerId = result.ExternalCustomerId;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(profile);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(Guid tenantId, Guid planId, BillingPeriod period, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var profile = await GetOrCreateBillingProfileAsync(tenantId, cancellationToken);
            if (profile == null || string.IsNullOrEmpty(profile.ExternalCustomerId))
                throw new InvalidOperationException("No se pudo obtener o crear el perfil de facturación con cliente externo.");

            var plan = await _db.SubscriptionPlans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, cancellationToken);
            if (plan == null || !plan.IsActive)
                throw new InvalidOperationException("Plan no encontrado o inactivo.");

            var priceId = period == BillingPeriod.Annual && !string.IsNullOrEmpty(plan.StripePriceIdAnnual)
                ? plan.StripePriceIdAnnual
                : plan.StripePriceIdMonthly;
            if (string.IsNullOrEmpty(priceId))
                throw new InvalidOperationException("El plan no tiene un precio configurado en Stripe. Configure StripePriceIdMonthly o StripePriceIdAnnual.");

            return await _provider.CreateCheckoutSessionAsync(new CreateCheckoutSessionRequest(
                profile.ExternalCustomerId,
                priceId,
                tenantId,
                planId,
                period,
                successUrl,
                cancelUrl), cancellationToken);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task ChangePlanAsync(Guid tenantId, Guid newPlanId, bool prorate, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var sub = await GetOperationalSubscriptionAsync(tenantId, cancellationToken);
            if (sub == null || string.IsNullOrEmpty(sub.ExternalSubscriptionId))
                throw new InvalidOperationException("No hay suscripción activa externa para cambiar.");

            var plan = await _db.SubscriptionPlans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == newPlanId && !p.IsDeleted, cancellationToken);
            if (plan == null || !plan.IsActive)
                throw new InvalidOperationException("Plan destino no encontrado o inactivo.");

            var priceId = sub.BillingPeriod == BillingPeriod.Annual && !string.IsNullOrEmpty(plan.StripePriceIdAnnual)
                ? plan.StripePriceIdAnnual
                : plan.StripePriceIdMonthly;
            if (string.IsNullOrEmpty(priceId))
                throw new InvalidOperationException("El plan no tiene precio Stripe configurado.");

            await _provider.ChangePlanAsync(new ChangeSubscriptionPlanRequest(sub.ExternalSubscriptionId, priceId, prorate), cancellationToken);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task CancelSubscriptionAsync(Guid tenantId, bool atPeriodEnd, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var sub = await GetOperationalSubscriptionAsync(tenantId, cancellationToken);
            if (sub == null || string.IsNullOrEmpty(sub.ExternalSubscriptionId))
                throw new InvalidOperationException("No hay suscripción activa externa.");

            await _provider.CancelSubscriptionAsync(new CancelSubscriptionRequest(sub.ExternalSubscriptionId, atPeriodEnd), cancellationToken);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task ResumeSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var sub = await GetOperationalSubscriptionAsync(tenantId, cancellationToken);
            if (sub == null || string.IsNullOrEmpty(sub.ExternalSubscriptionId))
                throw new InvalidOperationException("No hay suscripción para reactivar.");

            await _provider.ResumeSubscriptionAsync(new ResumeSubscriptionRequest(sub.ExternalSubscriptionId), cancellationToken);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task UpdateBillingProfileAsync(Guid tenantId, TenantBillingProfileUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var profile = await _db.TenantBillingProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && !b.IsDeleted, cancellationToken);
            if (profile == null)
                throw new InvalidOperationException("Perfil de facturación no encontrado.");

            if (dto.BillingEmail != null) profile.BillingEmail = NullIfEmpty(dto.BillingEmail);
            if (dto.LegalName != null) profile.LegalName = NullIfEmpty(dto.LegalName);
            if (dto.TaxId != null) profile.TaxId = NullIfEmpty(dto.TaxId);
            if (dto.Country != null) profile.Country = NullIfEmpty(dto.Country);
            if (dto.StateProvince != null) profile.StateProvince = NullIfEmpty(dto.StateProvince);
            if (dto.City != null) profile.City = NullIfEmpty(dto.City);
            if (dto.AddressLine1 != null) profile.AddressLine1 = NullIfEmpty(dto.AddressLine1);
            if (dto.AddressLine2 != null) profile.AddressLine2 = NullIfEmpty(dto.AddressLine2);
            if (dto.PostalCode != null) profile.PostalCode = NullIfEmpty(dto.PostalCode);
            if (dto.PreferredCurrency != null) profile.PreferredCurrency = NullIfEmpty(dto.PreferredCurrency);

            await _db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    internal async Task<TenantSubscription?> GetOperationalSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static TenantBillingProfileDto MapToDto(TenantBillingProfile b) =>
        new(b.Id, b.TenantId, b.BillingEmail, b.LegalName, b.TaxId, b.Country, b.AddressLine1, b.ExternalCustomerId, b.PreferredCurrency);
}
