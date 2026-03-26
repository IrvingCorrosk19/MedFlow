using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class SubscriptionPlanAdminService : ISubscriptionPlanAdminService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionPlanAdminService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionPlanListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SubscriptionPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new SubscriptionPlanListDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                MonthlyPrice = p.MonthlyPrice,
                Currency = p.Currency,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlanEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (p == null) return null;

        return new SubscriptionPlanEditDto
        {
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            MonthlyPrice = p.MonthlyPrice,
            AnnualPrice = p.AnnualPrice,
            Currency = p.Currency,
            MaxUsers = p.MaxUsers,
            MaxDoctors = p.MaxDoctors,
            MaxPatients = p.MaxPatients,
            MaxAppointmentsPerMonth = p.MaxAppointmentsPerMonth,
            MaxBranches = p.MaxBranches,
            IncludesBillingModule = p.IncludesBillingModule,
            IncludesAutomationModule = p.IncludesAutomationModule,
            IncludesReportsModule = p.IncludesReportsModule,
            IncludesPatientPortal = p.IncludesPatientPortal,
            IncludesMultiBranch = p.IncludesMultiBranch,
            IncludesAdvancedAnalytics = p.IncludesAdvancedAnalytics,
            TrialDays = p.TrialDays,
            IsActive = p.IsActive,
            SortOrder = p.SortOrder,
            StripePriceIdMonthly = p.StripePriceIdMonthly,
            StripePriceIdAnnual = p.StripePriceIdAnnual,
            StripeProductId = p.StripeProductId
        };
    }

    public async Task<Guid> CreateAsync(SubscriptionPlanEditDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToLowerInvariant();
        var dup = await _db.SubscriptionPlans.IgnoreQueryFilters().AnyAsync(p => p.Code == code && !p.IsDeleted, cancellationToken);
        if (dup)
            throw new InvalidOperationException("Ya existe un plan con ese código.");

        var p = MapToEntity(dto, new SubscriptionPlan { Code = code });
        _db.SubscriptionPlans.Add(p);
        await _db.SaveChangesAsync(cancellationToken);
        return p.Id;
    }

    public async Task UpdateAsync(Guid id, SubscriptionPlanEditDto dto, CancellationToken cancellationToken = default)
    {
        var p = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (p == null) throw new InvalidOperationException("Plan no encontrado.");

        var code = dto.Code.Trim().ToLowerInvariant();
        var dup = await _db.SubscriptionPlans.IgnoreQueryFilters()
            .AnyAsync(x => x.Code == code && x.Id != id && !x.IsDeleted, cancellationToken);
        if (dup)
            throw new InvalidOperationException("Ya existe otro plan con ese código.");

        p.Code = code;
        MapToEntity(dto, p);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SubscriptionPlan MapToEntity(SubscriptionPlanEditDto dto, SubscriptionPlan p)
    {
        p.Name = dto.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        p.MonthlyPrice = dto.MonthlyPrice;
        p.AnnualPrice = dto.AnnualPrice;
        p.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant();
        p.MaxUsers = dto.MaxUsers;
        p.MaxDoctors = dto.MaxDoctors;
        p.MaxPatients = dto.MaxPatients;
        p.MaxAppointmentsPerMonth = dto.MaxAppointmentsPerMonth;
        p.MaxBranches = dto.MaxBranches;
        p.IncludesBillingModule = dto.IncludesBillingModule;
        p.IncludesAutomationModule = dto.IncludesAutomationModule;
        p.IncludesReportsModule = dto.IncludesReportsModule;
        p.IncludesPatientPortal = dto.IncludesPatientPortal;
        p.IncludesMultiBranch = dto.IncludesMultiBranch;
        p.IncludesAdvancedAnalytics = dto.IncludesAdvancedAnalytics;
        p.TrialDays = dto.TrialDays;
        p.IsActive = dto.IsActive;
        p.SortOrder = dto.SortOrder;
        p.StripePriceIdMonthly = string.IsNullOrWhiteSpace(dto.StripePriceIdMonthly) ? null : dto.StripePriceIdMonthly.Trim();
        p.StripePriceIdAnnual = string.IsNullOrWhiteSpace(dto.StripePriceIdAnnual) ? null : dto.StripePriceIdAnnual.Trim();
        p.StripeProductId = string.IsNullOrWhiteSpace(dto.StripeProductId) ? null : dto.StripeProductId.Trim();
        return p;
    }
}
