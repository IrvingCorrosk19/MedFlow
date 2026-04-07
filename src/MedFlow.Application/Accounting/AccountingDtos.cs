using MedFlow.Domain.Enums;

namespace MedFlow.Application.Accounting;

// ── Chart of Accounts ─────────────────────────────────────────────────────────

public sealed record AccountListItemDto(
    Guid Id,
    string Code,
    string Name,
    AccountType Type,
    string TypeLabel,
    Guid? ParentId,
    string? ParentCode,
    bool AllowsDirectPosting,
    decimal CurrentBalance,
    int Level);

public sealed record AccountFormDto(
    Guid? Id,
    string Code,
    string Name,
    string? Description,
    AccountType Type,
    Guid? ParentId,
    bool AllowsDirectPosting);

// ── Fiscal Periods ────────────────────────────────────────────────────────────

public sealed record FiscalPeriodDto(
    Guid Id,
    int Year,
    int Month,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    FiscalPeriodStatus Status,
    int JournalEntryCount,
    bool IsYearlyClosed);

// ── Journal Entries ───────────────────────────────────────────────────────────

public sealed record JournalEntryListItemDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    string FiscalPeriodName,
    string Description,
    string? Reference,
    JournalEntryStatus Status,
    JournalEntryOrigin Origin,
    decimal TotalDebit,
    decimal TotalCredit,
    string CreatedByUserName);

public sealed record JournalEntryDetailDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    Guid FiscalPeriodId,
    string FiscalPeriodName,
    string Description,
    string? Reference,
    JournalEntryStatus Status,
    JournalEntryOrigin Origin,
    Guid? SourceDocumentId,
    decimal TotalDebit,
    decimal TotalCredit,
    string CreatedByUserName,
    DateTime? PostedAt,
    IReadOnlyList<JournalEntryLineDto> Lines,
    string? UpdatedByUserId = null);

public sealed record JournalEntryLineDto(
    Guid? Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? Description,
    decimal Debit,
    decimal Credit,
    int LineOrder);

public sealed record JournalEntryFormDto(
    Guid? Id,
    DateTime EntryDate,
    Guid FiscalPeriodId,
    string Description,
    string? Reference,
    List<JournalEntryLineFormDto> Lines);

public sealed record JournalEntryLineFormDto(
    Guid? Id,
    Guid AccountId,
    string? Description,
    decimal Debit,
    decimal Credit);

// ── Ledger ────────────────────────────────────────────────────────────────────

public sealed record LedgerAccountDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    decimal OpeningBalance,
    IReadOnlyList<LedgerLineDto> Lines,
    decimal ClosingBalance);

public sealed record LedgerLineDto(
    Guid JournalEntryId,
    string EntryNumber,
    DateTime EntryDate,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

// ── Trial Balance ─────────────────────────────────────────────────────────────

public sealed record TrialBalanceDto(
    DateTime AsOf,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record TrialBalanceLineDto(
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    decimal DebitBalance,
    decimal CreditBalance);

// ── Balance Sheet ─────────────────────────────────────────────────────────────

public sealed record BalanceSheetDto(
    DateTime AsOf,
    IReadOnlyList<BalanceSheetSectionDto> Assets,
    IReadOnlyList<BalanceSheetSectionDto> Liabilities,
    IReadOnlyList<BalanceSheetSectionDto> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity);

public sealed record BalanceSheetSectionDto(
    string AccountCode,
    string AccountName,
    decimal Balance);

// ── Income Statement (P&L) ────────────────────────────────────────────────────

public sealed record IncomeStatementDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<IncomeStatementLineDto> Revenue,
    IReadOnlyList<IncomeStatementLineDto> Costs,
    IReadOnlyList<IncomeStatementLineDto> Expenses,
    decimal TotalRevenue,
    decimal TotalCosts,
    decimal TotalExpenses,
    decimal GrossProfit,
    decimal NetIncome);

public sealed record IncomeStatementLineDto(
    string AccountCode,
    string AccountName,
    decimal Amount);

// ── Tax Rates ─────────────────────────────────────────────────────────────────

public sealed record TaxRateDto(
    Guid Id,
    string Name,
    string Code,
    decimal Rate,
    Guid? TaxAccountId,
    string? TaxAccountName,
    bool IsDefault,
    bool IsActive);

// ── Bank Accounts ─────────────────────────────────────────────────────────────

public sealed record BankAccountDto(
    Guid Id,
    string Name,
    string BankName,
    string? AccountNumber,
    string? Currency,
    decimal OpeningBalance,
    decimal CurrentBalance,
    Guid? AccountId,
    string? AccountCode,
    bool IsActive);

public sealed record BankTransactionDto(
    Guid Id,
    DateTime TransactionDate,
    BankTransactionType Type,
    decimal Amount,
    string? Description,
    string? Reference,
    bool IsReconciled,
    string? JournalEntryNumber);
