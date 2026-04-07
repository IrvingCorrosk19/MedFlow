using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for LedgerService covering trial balance, balance sheet,
/// income statement, and per-account ledger with running balance.
/// </summary>
public class LedgerServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static LedgerService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
        => new LedgerService(db);

    /// <summary>Creates a minimal Account and persists it.</summary>
    private static async Task<Account> SeedAccountAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        string code,
        string name,
        AccountType type,
        bool allowsDirectPosting = true)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Type = type,
            AllowsDirectPosting = allowsDirectPosting
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    /// <summary>
    /// Seeds a posted JournalEntry with a single pair of lines (debit on accountDebitId,
    /// credit on accountCreditId) and returns the entry.
    /// </summary>
    private static async Task<JournalEntry> SeedPostedEntryAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        Guid accountDebitId,
        Guid accountCreditId,
        decimal amount,
        DateTime entryDate)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = $"AST-{entryDate.Year}-{Guid.NewGuid():N}".Substring(0, 20),
            FiscalPeriodId = Guid.NewGuid(), // no period constraint in ledger queries
            EntryDate = entryDate,
            Description = "Seeded test entry",
            Status = JournalEntryStatus.Posted,
            CreatedByUserId = "seed",
            TotalDebit = amount,
            TotalCredit = amount
        };

        var debitLine = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            AccountId = accountDebitId,
            Debit = amount,
            Credit = 0m,
            LineOrder = 1
        };

        var creditLine = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            AccountId = accountCreditId,
            Debit = 0m,
            Credit = amount,
            LineOrder = 2
        };

        db.JournalEntries.Add(entry);
        db.JournalEntryLines.Add(debitLine);
        db.JournalEntryLines.Add(creditLine);
        await db.SaveChangesAsync();
        return entry;
    }

    /// <summary>
    /// Seeds a posted entry with one line only (for ledger running-balance tests).
    /// </summary>
    private static async Task SeedSingleLineEntryAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        Guid accountId,
        decimal debit,
        decimal credit,
        DateTime entryDate,
        string entryNumber)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = entryNumber,
            FiscalPeriodId = Guid.NewGuid(),
            EntryDate = entryDate,
            Description = "Running balance test",
            Status = JournalEntryStatus.Posted,
            CreatedByUserId = "seed",
            TotalDebit = debit,
            TotalCredit = credit
        };

        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            AccountId = accountId,
            Debit = debit,
            Credit = credit,
            LineOrder = 1
        };

        db.JournalEntries.Add(entry);
        db.JournalEntryLines.Add(line);
        await db.SaveChangesAsync();
    }

    // ── GetTrialBalanceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTrialBalanceAsync_EmptyTenant_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var result = await svc.GetTrialBalanceAsync(tenantId, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.Empty(result.Lines);
        Assert.Equal(0m, result.TotalDebit);
        Assert.Equal(0m, result.TotalCredit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_PostedEntries_ShowCorrectBalance()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Seed two accounts: Asset (debit-normal) and Revenue (credit-normal)
        var assetAccount = await SeedAccountAsync(db, tenantId, "1100", "Cash", AccountType.Asset);
        var revenueAccount = await SeedAccountAsync(db, tenantId, "4100", "Service Revenue", AccountType.Revenue);

        // Post one entry: 500 debit on Cash, 500 credit on Revenue
        var entryDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        await SeedPostedEntryAsync(db, tenantId, assetAccount.Id, revenueAccount.Id, 500m, entryDate);

        var asOf = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var result = await svc.GetTrialBalanceAsync(tenantId, asOf);

        Assert.Equal(2, result.Lines.Count);

        var cashLine = result.Lines.Single(l => l.AccountCode == "1100");
        var revLine = result.Lines.Single(l => l.AccountCode == "4100");

        // Asset has debit balance of 500
        Assert.Equal(500m, cashLine.DebitBalance);
        Assert.Equal(0m, cashLine.CreditBalance);

        // Revenue has credit balance of 500 (credit - debit = 500)
        // LedgerService normalisation: both types end up using (Max(0, debit-credit), Max(0, credit-debit))
        // For Revenue: debit=0, credit=500 → debitBal=0, creditBal=500
        Assert.Equal(0m, revLine.DebitBalance);
        Assert.Equal(500m, revLine.CreditBalance);

        // Totals balance (debit total == credit total for a well-posted set of entries)
        Assert.Equal(result.TotalDebit, result.TotalCredit);
    }

    // ── GetBalanceSheetAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBalanceSheetAsync_ReturnsCorrectSections()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var assetAccount = await SeedAccountAsync(db, tenantId, "1100", "Cash", AccountType.Asset);
        var liabilityAccount = await SeedAccountAsync(db, tenantId, "2100", "Accounts Payable", AccountType.Liability);
        var equityAccount = await SeedAccountAsync(db, tenantId, "3100", "Capital", AccountType.Equity);

        var entryDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        // Debit Asset / Credit Liability → 300
        await SeedPostedEntryAsync(db, tenantId, assetAccount.Id, liabilityAccount.Id, 300m, entryDate);
        // Debit Asset / Credit Equity → 200
        await SeedPostedEntryAsync(db, tenantId, assetAccount.Id, equityAccount.Id, 200m, entryDate);

        var asOf = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var result = await svc.GetBalanceSheetAsync(tenantId, asOf);

        // Assets: Cash debited 500 total
        Assert.Single(result.Assets);
        Assert.Equal(500m, result.TotalAssets);

        // Liabilities: AP credited 300
        Assert.Single(result.Liabilities);
        Assert.Equal(300m, result.TotalLiabilities);

        // Equity: Capital credited 200
        // (NetIncome = 0 because no revenue/expense accounts)
        Assert.Single(result.Equity);
        Assert.Equal(200m, result.TotalEquity);
    }

    // ── GetIncomeStatementAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetIncomeStatementAsync_ReturnsRevenueAndExpenses()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var revenueAccount = await SeedAccountAsync(db, tenantId, "4100", "Service Revenue", AccountType.Revenue);
        var expenseAccount = await SeedAccountAsync(db, tenantId, "5100", "Rent Expense", AccountType.Expense);
        // Need a counterpart account for double-entry (Asset account, not reported in P&L)
        var assetAccount = await SeedAccountAsync(db, tenantId, "1100", "Cash", AccountType.Asset);

        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var entryDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Revenue: debit Asset 1000, credit Revenue 1000
        await SeedPostedEntryAsync(db, tenantId, assetAccount.Id, revenueAccount.Id, 1000m, entryDate);
        // Expense: debit Expense 400, credit Asset 400
        await SeedPostedEntryAsync(db, tenantId, expenseAccount.Id, assetAccount.Id, 400m, entryDate);

        var result = await svc.GetIncomeStatementAsync(tenantId, from, to);

        Assert.Equal(1000m, result.TotalRevenue);
        Assert.Equal(400m, result.TotalExpenses);
        Assert.Equal(0m, result.TotalCosts);
        Assert.Equal(1000m, result.GrossProfit);   // revenue - costs (no costs)
        Assert.Equal(600m, result.NetIncome);      // grossProfit - expenses

        Assert.Single(result.Revenue);
        Assert.Single(result.Expenses);
        Assert.Empty(result.Costs);

        Assert.Equal(1000m, result.Revenue[0].Amount);
        Assert.Equal(400m, result.Expenses[0].Amount);
    }

    // ── GetLedgerAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLedgerAsync_RunningBalance_IsCorrect()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Asset account: debit-normal
        var account = await SeedAccountAsync(db, tenantId, "1100", "Cash", AccountType.Asset);

        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Three entries:
        //   Day 1: Debit 100  → running = 100
        //   Day 2: Debit  50  → running = 150
        //   Day 3: Credit 30  → running = 120
        await SeedSingleLineEntryAsync(db, tenantId, account.Id, 100m, 0m, baseDate.AddDays(0), "AST-2026-000001");
        await SeedSingleLineEntryAsync(db, tenantId, account.Id, 50m, 0m, baseDate.AddDays(1), "AST-2026-000002");
        await SeedSingleLineEntryAsync(db, tenantId, account.Id, 0m, 30m, baseDate.AddDays(2), "AST-2026-000003");

        var from = baseDate;
        var to = baseDate.AddDays(30);

        var result = await svc.GetLedgerAsync(tenantId, account.Id, from, to);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Lines.Count);

        // Verify running balances in order
        Assert.Equal(100m, result.Lines[0].RunningBalance);
        Assert.Equal(150m, result.Lines[1].RunningBalance);
        Assert.Equal(120m, result.Lines[2].RunningBalance);

        // Closing balance should match the last running balance
        Assert.Equal(120m, result.ClosingBalance);
    }
}
