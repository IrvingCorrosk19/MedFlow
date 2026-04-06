using MedFlow.Application.Accounting;

namespace MedFlow.Application.Interfaces;

public interface ILedgerService
{
    Task<LedgerAccountDto?> GetLedgerAsync(
        Guid tenantId,
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task<TrialBalanceDto> GetTrialBalanceAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken ct = default);

    Task<BalanceSheetDto> GetBalanceSheetAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken ct = default);

    Task<IncomeStatementDto> GetIncomeStatementAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}
