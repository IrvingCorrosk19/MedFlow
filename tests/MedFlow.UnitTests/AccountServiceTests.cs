using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for AccountService covering creation, tenant isolation,
/// soft-delete, and code generation.
/// </summary>
public class AccountServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static AccountService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
        => new AccountService(db);

    /// <summary>Builds a minimal Account entity for the given tenant.</summary>
    private static Account BuildAccount(
        Guid tenantId,
        string code,
        string name,
        AccountType type = AccountType.Asset,
        bool allowsDirectPosting = true)
        => new Account
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Type = type,
            AllowsDirectPosting = allowsDirectPosting
        };

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidAccount_Persists()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var account = BuildAccount(tenantId, "1100", "Cash");
        var created = await svc.CreateAsync(account);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(tenantId, created.TenantId);
        Assert.Equal("1100", created.Code);
        Assert.Equal("Cash", created.Name);

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("1100", fetched!.Code);
    }

    // ── GetAllAsync tenant isolation ───────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTenantAccounts()
    {
        var (dbA, dbB) = DbContextFactory.CreateTwentyTenants(out var tenantAId, out var tenantBId);
        var svcA = CreateService(dbA);
        var svcB = CreateService(dbB);

        // Seed two accounts for tenant A and one for tenant B in the same DB
        await svcA.CreateAsync(BuildAccount(tenantAId, "1100", "Cash A", AccountType.Asset));
        await svcA.CreateAsync(BuildAccount(tenantAId, "1200", "Receivables A", AccountType.Asset));
        await svcB.CreateAsync(BuildAccount(tenantBId, "1100", "Cash B", AccountType.Asset));

        var tenantAAccounts = await svcA.GetAllAsync(tenantAId);
        var tenantBAccounts = await svcB.GetAllAsync(tenantBId);

        Assert.Equal(2, tenantAAccounts.Count);
        Assert.Single(tenantBAccounts);

        // Verify no cross-tenant leakage
        Assert.All(tenantAAccounts, a => Assert.Contains("A", a.Name));
        Assert.All(tenantBAccounts, a => Assert.Contains("B", a.Name));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ValidAccount_SetsIsDeleted()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var account = await svc.CreateAsync(BuildAccount(tenantId, "5100", "Rent Expense", AccountType.Expense));

        await svc.DeleteAsync(account.Id);

        // GetByIdAsync uses the !IsDeleted query filter → returns null for deleted entities.
        // Verify the flag directly via IgnoreQueryFilters.
        var deleted = await db.Accounts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == account.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
    }

    // ── GenerateNextCodeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateNextCodeAsync_NoExistingCodes_ReturnsBaseCode()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // No accounts exist yet — should return prefix + "00"
        var code = await svc.GenerateNextCodeAsync(tenantId, "11");

        Assert.Equal("1100", code);
    }

    [Fact]
    public async Task GenerateNextCodeAsync_ReturnsNextSequential()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Seed two accounts with the prefix "11"
        await svc.CreateAsync(BuildAccount(tenantId, "1100", "Cash"));
        await svc.CreateAsync(BuildAccount(tenantId, "1101", "Petty Cash"));

        // The service orders by Code descending and parses the suffix:
        // Last code is "1101" → suffix = "01" → next = "1102"
        var next = await svc.GenerateNextCodeAsync(tenantId, "11");

        Assert.Equal("1102", next);
    }

    [Fact]
    public async Task GenerateNextCodeAsync_WithTwoDigitSuffix_IncrementsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Seed accounts 1100 through 1109
        for (int i = 0; i <= 9; i++)
        {
            await svc.CreateAsync(BuildAccount(tenantId, $"110{i}", $"Account {i}"));
        }

        // Last code = "1109" → suffix = "09" → next = "1110"
        var next = await svc.GenerateNextCodeAsync(tenantId, "11");

        Assert.Equal("1110", next);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ChangesNameAndCode_Persists()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var account = await svc.CreateAsync(BuildAccount(tenantId, "1100", "Old Name"));

        account.Name = "New Name";
        account.Code = "1199";
        await svc.UpdateAsync(account);

        var updated = await svc.GetByIdAsync(account.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("1199", updated.Code);
    }

    // ── RecalculateBalancesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RecalculateBalancesAsync_SetsCurrentBalance_FromPostedLines()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var account = await svc.CreateAsync(BuildAccount(tenantId, "1100", "Cash", AccountType.Asset));

        // Seed a posted journal entry with 300 debit on the account
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = "AST-2026-000001",
            FiscalPeriodId = Guid.NewGuid(),
            EntryDate = DateTime.UtcNow,
            Description = "Test",
            Status = JournalEntryStatus.Posted,
            CreatedByUserId = "seed",
            TotalDebit = 300m,
            TotalCredit = 300m
        };
        var lineDebit = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            AccountId = account.Id,
            Debit = 300m,
            Credit = 0m,
            LineOrder = 1
        };
        // Credit line on a dummy account (needed for balanced entry, not relevant to calculation)
        var dummyAccountId = Guid.NewGuid();
        var lineCredit = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            AccountId = dummyAccountId,
            Debit = 0m,
            Credit = 300m,
            LineOrder = 2
        };
        db.JournalEntries.Add(entry);
        db.JournalEntryLines.Add(lineDebit);
        db.JournalEntryLines.Add(lineCredit);
        await db.SaveChangesAsync();

        await svc.RecalculateBalancesAsync(tenantId);

        var updated = await svc.GetByIdAsync(account.Id);
        Assert.NotNull(updated);
        // Asset: debit-normal → CurrentBalance = TotalDebit - TotalCredit = 300 - 0 = 300
        Assert.Equal(300m, updated!.CurrentBalance);
    }
}
