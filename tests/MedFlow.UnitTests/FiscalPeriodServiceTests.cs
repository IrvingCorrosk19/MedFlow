using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for FiscalPeriodService covering creation, closing, and reopening periods.
/// </summary>
public class FiscalPeriodServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static FiscalPeriodService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
        => new FiscalPeriodService(
            db,
            Options.Create(new AccountMappingOptions()),
            NullLogger<FiscalPeriodService>.Instance);

    /// <summary>Seeds an open fiscal period directly and returns it.</summary>
    private static async Task<FiscalPeriod> SeedOpenPeriodAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId,
        int year = 2026, int month = 1)
    {
        var period = new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            Name = $"Test Period {year}-{month:D2}",
            StartDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(year, month, 28, 0, 0, 0, DateTimeKind.Utc),
            Status = FiscalPeriodStatus.Open
        };
        db.FiscalPeriods.Add(period);
        await db.SaveChangesAsync();
        return period;
    }

    /// <summary>Seeds a Draft JournalEntry linked to the given fiscal period.</summary>
    private static async Task SeedDraftEntryAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId, Guid fiscalPeriodId)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNumber = "AST-TEST-000001",
            FiscalPeriodId = fiscalPeriodId,
            EntryDate = DateTime.UtcNow,
            Description = "Draft entry",
            Status = JournalEntryStatus.Draft,
            CreatedByUserId = "user1",
            TotalDebit = 100m,
            TotalCredit = 100m
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    // ── GetOrCreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewPeriod_WhenNotExists()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var period = await svc.GetOrCreateAsync(tenantId, 2026, 3);

        Assert.NotEqual(Guid.Empty, period.Id);
        Assert.Equal(tenantId, period.TenantId);
        Assert.Equal(2026, period.Year);
        Assert.Equal(3, period.Month);
        Assert.Equal(FiscalPeriodStatus.Open, period.Status);

        // Verify persisted in DB
        var inDb = await svc.GetByIdAsync(period.Id);
        Assert.NotNull(inDb);
        Assert.Equal(2026, inDb!.Year);
        Assert.Equal(3, inDb.Month);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsExisting_WhenExists()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Create the period first
        var first = await svc.GetOrCreateAsync(tenantId, 2026, 4);

        // Call again with same year/month — should NOT create a duplicate
        var second = await svc.GetOrCreateAsync(tenantId, 2026, 4);

        Assert.Equal(first.Id, second.Id);

        // Confirm only one period exists for this tenant/year/month
        var all = await svc.GetAllAsync(tenantId);
        Assert.Single(all.Where(p => p.Year == 2026 && p.Month == 4));
    }

    // ── CloseAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_ClosedPeriod_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // Seed a period that is already closed
        var period = new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = 2025,
            Month = 12,
            Name = "Diciembre 2025",
            StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = FiscalPeriodStatus.Closed
        };
        db.FiscalPeriods.Add(period);
        await db.SaveChangesAsync();

        var (ok, error) = await svc.CloseAsync(period.Id, "user1");

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("cerrado", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseAsync_WithDraftEntries_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var period = await SeedOpenPeriodAsync(db, tenantId);
        await SeedDraftEntryAsync(db, tenantId, period.Id);

        var (ok, error) = await svc.CloseAsync(period.Id, "user1");

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("borrador", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseAsync_OpenPeriod_NoEntries_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var period = await SeedOpenPeriodAsync(db, tenantId);

        var (ok, error) = await svc.CloseAsync(period.Id, "user1");

        Assert.True(ok);
        Assert.Null(error);

        var closed = await svc.GetByIdAsync(period.Id);
        Assert.NotNull(closed);
        Assert.Equal(FiscalPeriodStatus.Closed, closed!.Status);
        Assert.NotNull(closed.ClosedAt);
        Assert.Equal("user1", closed.ClosedByUserId);
    }

    // ── ReopenAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReopenAsync_OpenPeriod_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var period = await SeedOpenPeriodAsync(db, tenantId);

        var (ok, error) = await svc.ReopenAsync(period.Id, "user1");

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("abierto", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReopenAsync_ClosedPeriod_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        // First create and close the period
        var period = await SeedOpenPeriodAsync(db, tenantId);
        var (closeOk, _) = await svc.CloseAsync(period.Id, "user1");
        Assert.True(closeOk);

        // Now reopen it
        var (ok, error) = await svc.ReopenAsync(period.Id, "user1");

        Assert.True(ok);
        Assert.Null(error);

        var reopened = await svc.GetByIdAsync(period.Id);
        Assert.NotNull(reopened);
        Assert.Equal(FiscalPeriodStatus.Open, reopened!.Status);
        Assert.Null(reopened.ClosedAt);
        Assert.Null(reopened.ClosedByUserId);
    }
}
