using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class SaasBillingQueryService : ISaasBillingQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly ISubscriptionLimitService _limits;

    public SaasBillingQueryService(ApplicationDbContext db, ISubscriptionLimitService limits)
    {
        _db = db;
        _limits = limits;
    }

    public async Task<SaasBillingOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .CountAsync(cancellationToken);

        var activeSubs = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .CountAsync(cancellationToken);

        var pastDue = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.Status == SubscriptionStatus.PastDue)
            .CountAsync(cancellationToken);

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mrr = await _db.TenantSubscriptions.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .Include(s => s.SubscriptionPlan)
            .SumAsync(s => s.BillingPeriod == BillingPeriod.Annual ? (s.SubscriptionPlan.AnnualPrice ?? s.SubscriptionPlan.MonthlyPrice * 12) / 12m : s.SubscriptionPlan.MonthlyPrice, cancellationToken);

        var paidThisMonth = await _db.SaaSBillingTransactions.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.Status == SaasTransactionStatus.Succeeded && t.TransactionType == SaasTransactionType.PaymentSucceeded && t.OccurredAt >= startOfMonth)
            .SumAsync(t => t.Amount, cancellationToken);

        var failedThisMonth = await _db.SaaSBillingTransactions.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.Status == SaasTransactionStatus.Failed && t.OccurredAt >= startOfMonth)
            .CountAsync(cancellationToken);

        return new SaasBillingOverviewDto(tenants, activeSubs, pastDue, mrr, paidThisMonth, failedThisMonth);
    }

    public async Task<IReadOnlyList<SaasTransactionListItemDto>> GetTransactionsAsync(int skip, int take, Guid? tenantId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var q = _db.SaaSBillingTransactions.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .Include(t => t.Tenant)
            .AsQueryable();

        if (tenantId.HasValue)
            q = q.Where(t => t.TenantId == tenantId.Value);
        if (from.HasValue)
            q = q.Where(t => t.OccurredAt >= from.Value);
        if (to.HasValue)
            q = q.Where(t => t.OccurredAt <= to.Value);

        return await q.OrderByDescending(t => t.OccurredAt)
            .Skip(skip).Take(take)
            .Select(t => new SaasTransactionListItemDto(
                t.Id, t.TenantId, t.Tenant.Name, t.Tenant.Code, t.TransactionType, t.Status,
                t.Amount, t.Currency, t.OccurredAt, t.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SaaSInvoiceListItemDto>> GetInvoicesAsync(int skip, int take, Guid? tenantId, SaaSInvoiceStatus? status, CancellationToken cancellationToken = default)
    {
        var q = _db.SaaSInvoices.IgnoreQueryFilters()
            .Where(i => !i.IsDeleted)
            .Include(i => i.Tenant)
            .AsQueryable();

        if (tenantId.HasValue)
            q = q.Where(i => i.TenantId == tenantId.Value);
        if (status.HasValue)
            q = q.Where(i => i.Status == status.Value);

        return await q.OrderByDescending(i => i.IssueDate)
            .Skip(skip).Take(take)
            .Select(i => new SaaSInvoiceListItemDto(
                i.Id, i.TenantId, i.Tenant.Name, i.InvoiceNumber, i.TotalAmount, i.Currency,
                i.Status, i.IssueDate, i.DueDate, i.InvoiceUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantBillingPortalDto?> GetTenantBillingPortalAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sub = await _db.TenantSubscriptions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Where(s => s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub == null) return null;

        var usage = await _limits.GetCurrentUsageAsync(tenantId, cancellationToken);
        var invoices = await GetInvoicesAsync(0, 10, tenantId, null, cancellationToken);
        var transactions = await GetTransactionsAsync(0, 10, tenantId, null, null, cancellationToken);
        var hasProfile = await _db.TenantBillingProfiles.IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == tenantId && p.IsActive, cancellationToken);

        return new TenantBillingPortalDto(
            sub.SubscriptionPlan.Name,
            sub.SubscriptionPlan.Code,
            sub.Status,
            sub.BillingPeriod,
            sub.NextBillingDate,
            sub.CurrentPeriodStart,
            sub.CurrentPeriodEnd,
            !string.IsNullOrEmpty(sub.ExternalSubscriptionId),
            sub.CancelAtPeriodEnd,
            usage,
            invoices,
            transactions,
            hasProfile);
    }
}
