using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for BankAccountService covering creation, tenant isolation,
/// balance tracking via transactions, and reconciliation.
/// </summary>
public class BankAccountServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static BankAccountService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
        => new BankAccountService(db);

    /// <summary>Builds and returns a minimal BankAccount for the given tenant.</summary>
    private static BankAccount BuildBankAccount(Guid tenantId, string name = "Test Bank Account")
        => new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            BankName = "Test Bank",
            AccountNumber = "ACC-001",
            Currency = "USD",
            OpeningBalance = 0m
        };

    /// <summary>Creates a bank account via the service and returns it.</summary>
    private static async Task<BankAccount> CreateAccountAsync(
        BankAccountService svc, Guid tenantId, string name = "Test Bank Account")
    {
        var account = BuildBankAccount(tenantId, name);
        return await svc.CreateAsync(account);
    }

    /// <summary>Seeds a posted JournalEntry directly into the DB (for reconciliation tests).</summary>
    private static async Task<JournalEntry> SeedPostedJournalEntryAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = "AST-2026-000001",
            FiscalPeriodId = Guid.NewGuid(),
            EntryDate = DateTime.UtcNow,
            Description = "Test posted entry",
            Status = JournalEntryStatus.Posted,
            CreatedByUserId = "seed",
            TotalDebit = 500m,
            TotalCredit = 500m
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidAccount_Persists()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var account = BuildBankAccount(tenantId);
        var created = await svc.CreateAsync(account);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(tenantId, created.TenantId);
        Assert.Equal("Test Bank Account", created.Name);

        // Verify retrievable by ID with correct tenant
        var fetched = await svc.GetByIdAsync(created.Id, tenantId);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    // ── GetByIdAsync tenant isolation ─────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WrongTenant_ReturnsNull()
    {
        var (dbA, dbB) = DbContextFactory.CreateTwentyTenants(out var tenantAId, out var tenantBId);
        var svcA = CreateService(dbA);
        var svcB = CreateService(dbB);

        var accountA = await CreateAccountAsync(svcA, tenantAId, "TenantA Account");

        // Fetch tenant A's account using tenant B's service — must return null
        var result = await svcB.GetByIdAsync(accountA.Id, tenantBId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_CorrectTenant_ReturnsAccount()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var created = await CreateAccountAsync(svc, tenantId);

        var fetched = await svc.GetByIdAsync(created.Id, tenantId);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(tenantId, fetched.TenantId);
    }

    // ── AddTransactionAsync & balance ──────────────────────────────────────────

    [Fact]
    public async Task AddTransactionAsync_Credit_IncreasesBalance()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Create a bank account with opening balance of 1000
        var bankAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Main Account",
            BankName = "National Bank",
            AccountNumber = "ACC-001",
            Currency = "USD",
            OpeningBalance = 1000m
        };
        await svc.CreateAsync(bankAccount);

        // Add a credit transaction of 500
        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccount.Id,
            TransactionDate = DateTime.UtcNow,
            Type = BankTransactionType.Credit,
            Amount = 500m,
            Description = "Deposit"
        };
        await svc.AddTransactionAsync(tx);

        // GetAllAsync computes the current balance as opening + credits - debits
        var all = await svc.GetAllAsync(tenantId);
        var dto = all.Single(b => b.Id == bankAccount.Id);

        Assert.Equal(1500m, dto.CurrentBalance); // 1000 opening + 500 credit
    }

    [Fact]
    public async Task AddTransactionAsync_Debit_DecreasesBalance()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var bankAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Main Account",
            BankName = "National Bank",
            AccountNumber = "ACC-002",
            Currency = "USD",
            OpeningBalance = 2000m
        };
        await svc.CreateAsync(bankAccount);

        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccount.Id,
            TransactionDate = DateTime.UtcNow,
            Type = BankTransactionType.Debit,
            Amount = 300m,
            Description = "Payment"
        };
        await svc.AddTransactionAsync(tx);

        var all = await svc.GetAllAsync(tenantId);
        var dto = all.Single(b => b.Id == bankAccount.Id);

        Assert.Equal(1700m, dto.CurrentBalance); // 2000 opening - 300 debit
    }

    // ── ReconcileAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReconcileAsync_ValidTransaction_SetsReconciledTrue()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var bankAccount = await CreateAccountAsync(svc, tenantId);

        // Seed a bank transaction (unreconciled)
        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccount.Id,
            TransactionDate = DateTime.UtcNow,
            Type = BankTransactionType.Credit,
            Amount = 500m,
            Description = "To reconcile",
            IsReconciled = false
        };
        await svc.AddTransactionAsync(tx);

        // Seed a posted journal entry
        var entry = await SeedPostedJournalEntryAsync(db, tenantId);

        var (ok, error) = await svc.ReconcileAsync(tx.Id, entry.Id);

        Assert.True(ok);
        Assert.Null(error);

        // Verify the transaction is now marked as reconciled
        var transactions = await svc.GetTransactionsAsync(bankAccount.Id, null, null, true);
        var reconciled = transactions.Single(t => t.Id == tx.Id);
        Assert.True(reconciled.IsReconciled);
    }

    [Fact]
    public async Task ReconcileAsync_AlreadyReconciled_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var bankAccount = await CreateAccountAsync(svc, tenantId);

        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccount.Id,
            TransactionDate = DateTime.UtcNow,
            Type = BankTransactionType.Credit,
            Amount = 500m,
            Description = "Already reconciled",
            IsReconciled = false
        };
        await svc.AddTransactionAsync(tx);

        var entry = await SeedPostedJournalEntryAsync(db, tenantId);

        // First reconciliation — succeeds
        var (firstOk, _) = await svc.ReconcileAsync(tx.Id, entry.Id);
        Assert.True(firstOk);

        // Second reconciliation — must return error
        var (ok, error) = await svc.ReconcileAsync(tx.Id, entry.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("conciliada", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReconcileAsync_NonPostedJournalEntry_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var bankAccount = await CreateAccountAsync(svc, tenantId);

        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccount.Id,
            TransactionDate = DateTime.UtcNow,
            Type = BankTransactionType.Credit,
            Amount = 200m,
            IsReconciled = false
        };
        await svc.AddTransactionAsync(tx);

        // Seed a DRAFT journal entry (not posted)
        var draftEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = "AST-2026-000002",
            FiscalPeriodId = Guid.NewGuid(),
            EntryDate = DateTime.UtcNow,
            Description = "Draft entry",
            Status = JournalEntryStatus.Draft,
            CreatedByUserId = "seed",
            TotalDebit = 200m,
            TotalCredit = 200m
        };
        db.JournalEntries.Add(draftEntry);
        await db.SaveChangesAsync();

        var (ok, error) = await svc.ReconcileAsync(tx.Id, draftEntry.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("contabilizado", error, StringComparison.OrdinalIgnoreCase);
    }
}
