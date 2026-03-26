using MedFlow.Application.Saas;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces;

public interface ISaasBillingQueryService
{
    Task<SaasBillingOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaasTransactionListItemDto>> GetTransactionsAsync(int skip, int take, Guid? tenantId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaaSInvoiceListItemDto>> GetInvoicesAsync(int skip, int take, Guid? tenantId, SaaSInvoiceStatus? status, CancellationToken cancellationToken = default);
    Task<TenantBillingPortalDto?> GetTenantBillingPortalAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantBillingPortalDto(
    string PlanName,
    string PlanCode,
    SubscriptionStatus SubscriptionStatus,
    BillingPeriod BillingPeriod,
    DateTime? NextBillingDate,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool HasExternalSubscription,
    bool CancelAtPeriodEnd,
    TenantUsageDto Usage,
    IReadOnlyList<SaaSInvoiceListItemDto> RecentInvoices,
    IReadOnlyList<SaasTransactionListItemDto> RecentTransactions,
    bool HasBillingProfile);

public sealed record SaasBillingOverviewDto(
    int TotalTenants,
    int ActiveSubscriptions,
    int PastDueCount,
    decimal MonthlyRecurringRevenue,
    decimal TotalPaidThisMonth,
    int FailedPaymentsThisMonth);

public sealed record SaasTransactionListItemDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string TenantCode,
    SaasTransactionType Type,
    SaasTransactionStatus Status,
    decimal Amount,
    string Currency,
    DateTime OccurredAt,
    string? Description);

public sealed record SaaSInvoiceListItemDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string InvoiceNumber,
    decimal TotalAmount,
    string Currency,
    SaaSInvoiceStatus Status,
    DateTime IssueDate,
    DateTime? DueDate,
    string? InvoiceUrl);
