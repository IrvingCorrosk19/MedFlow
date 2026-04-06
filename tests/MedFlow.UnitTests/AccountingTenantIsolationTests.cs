using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Verifies that JournalEntryService and BankAccountService enforce tenant isolation
/// after the IDOR fix: fetching a record by ID must always scope the query to the
/// caller's tenantId and must never return a record belonging to a different tenant.
/// </summary>
public class AccountingTenantIsolationTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static JournalEntryService CreateJournalService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        // Constructor: JournalEntryService(IApplicationDbContext context, IAccountService accounts, ApplicationDbContext dbContext)
        // Pass the same db instance for both interface and concrete type.
        return new JournalEntryService(db, new Mock<IAccountService>().Object, db);
    }

    private static BankAccountService CreateBankAccountService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        return new BankAccountService(db);
    }

    /// <summary>Seeds a minimal open FiscalPeriod and a JournalEntry (no lines needed).</summary>
    private static async Task<JournalEntry> SeedJournalEntryAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId)
    {
        var period = new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = DateTime.UtcNow.Year,
            Month = DateTime.UtcNow.Month,
            Name = "Test Period",
            StartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 28, 0, 0, 0, DateTimeKind.Utc),
            Status = FiscalPeriodStatus.Open
        };
        db.FiscalPeriods.Add(period);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = $"AST-{DateTime.UtcNow.Year}-{Guid.NewGuid():N}"[..20],
            FiscalPeriodId = period.Id,
            EntryDate = DateTime.Today,
            Description = "Test journal entry",
            Status = JournalEntryStatus.Draft,
            Origin = JournalEntryOrigin.Manual,
            CreatedByUserId = "seed",
            TotalDebit = 100m,
            TotalCredit = 100m
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    /// <summary>Seeds a minimal BankAccount.</summary>
    private static async Task<BankAccount> SeedBankAccountAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId)
    {
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Banco de Prueba",
            BankName = "TestBank",
            AccountNumber = "001-000001",
            Currency = "USD",
            OpeningBalance = 0m
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    // ── JournalEntry isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task JournalEntry_GetDetailAsync_TenantA_CannotFetch_TenantB_Entry()
    {
        // Tenant B creates an entry; Tenant A must NOT be able to retrieve it by ID.
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var entryB = await SeedJournalEntryAsync(ctxB, tidB);

        // Service scoped to Tenant A
        var svcA = CreateJournalService(ctxA);
        var detail = await svcA.GetDetailAsync(entryB.Id, tidA);

        Assert.Null(detail);
    }

    [Fact]
    public async Task JournalEntry_GetByIdAsync_TenantA_CannotFetch_TenantB_Entry()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var entryB = await SeedJournalEntryAsync(ctxB, tidB);

        var svcA = CreateJournalService(ctxA);
        var result = await svcA.GetByIdAsync(entryB.Id, tidA);

        Assert.Null(result);
    }

    [Fact]
    public async Task JournalEntry_GetDetailAsync_TenantA_CanFetch_OwnEntry()
    {
        // Sanity: Tenant A must be able to fetch its own entry.
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);

        var entry = await SeedJournalEntryAsync(db, tenantId);

        var svc = CreateJournalService(db);
        var detail = await svc.GetDetailAsync(entry.Id, tenantId);

        Assert.NotNull(detail);
        Assert.Equal(entry.Id, detail!.Id);
    }

    // ── BankAccount isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task BankAccount_GetByIdAsync_TenantA_CannotFetch_TenantB_Account()
    {
        // Tenant B creates a bank account; Tenant A must NOT be able to retrieve it.
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var accountB = await SeedBankAccountAsync(ctxB, tidB);

        // BankAccountService queries with explicit tenantId predicate (IDOR fix)
        var svcA = CreateBankAccountService(ctxA);
        var result = await svcA.GetByIdAsync(accountB.Id, tidA);

        Assert.Null(result);
    }

    [Fact]
    public async Task BankAccount_GetByIdAsync_TenantA_CanFetch_OwnAccount()
    {
        // Sanity: Tenant A must be able to fetch its own bank account.
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);

        var account = await SeedBankAccountAsync(db, tenantId);

        var svc = CreateBankAccountService(db);
        var result = await svc.GetByIdAsync(account.Id, tenantId);

        Assert.NotNull(result);
        Assert.Equal(account.Id, result!.Id);
        Assert.Equal(tenantId, result.TenantId);
    }
}
