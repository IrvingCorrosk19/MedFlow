using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;

namespace MedFlow.UnitTests;

public class CashMovementServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (CashMovementService svc, MedFlow.Infrastructure.Persistence.ApplicationDbContext db) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        return (new CashMovementService(db), db);
    }

    [Fact]
    public async Task CreateAsync_StoresMovementWithTenantId()
    {
        var (svc, db) = Create();

        var movement = new CashMovement
        {
            TenantId = TenantId,
            MovementType = CashMovementType.Income,
            Amount = 150.00m,
            Description = "Consulta médica",
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = "user-1"
        };

        var (created, err) = await svc.CreateAsync(movement);

        Assert.Null(err);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal(150.00m, created.Amount);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsOnlyMatchingDates()
    {
        var (svc, _) = Create();

        var today = DateTime.UtcNow.Date;
        await svc.CreateAsync(new CashMovement { TenantId = TenantId, MovementType = CashMovementType.Income, Amount = 100, MovementDate = today, CreatedByUserId = "u" });
        await svc.CreateAsync(new CashMovement { TenantId = TenantId, MovementType = CashMovementType.Expense, Amount = 50, MovementDate = today.AddDays(-5), CreatedByUserId = "u" });

        var result = await svc.GetByDateRangeAsync(today, today.AddDays(1));

        Assert.Single(result);
        Assert.Equal(100, result[0].Amount);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMovement()
    {
        var (svc, db) = Create();

        var (created, _) = await svc.CreateAsync(new CashMovement
        {
            TenantId = TenantId,
            MovementType = CashMovementType.Adjustment,
            Amount = 25,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = "u"
        });

        var deleted = await svc.DeleteAsync(created!.Id);

        Assert.True(deleted);
        var found = await db.CashMovements.FindAsync(created.Id);
        Assert.Null(found); // hard delete
    }

    [Fact]
    public async Task GetDayTotalsAsync_CalculatesCorrectSums()
    {
        var (svc, _) = Create();

        var today = DateTime.UtcNow.Date;
        await svc.CreateAsync(new CashMovement { TenantId = TenantId, MovementType = CashMovementType.Income, Amount = 200, MovementDate = today, CreatedByUserId = "u" });
        await svc.CreateAsync(new CashMovement { TenantId = TenantId, MovementType = CashMovementType.Income, Amount = 100, MovementDate = today, CreatedByUserId = "u" });
        await svc.CreateAsync(new CashMovement { TenantId = TenantId, MovementType = CashMovementType.Expense, Amount = 80, MovementDate = today, CreatedByUserId = "u" });

        var (income, expense, adj) = await svc.GetDayTotalsAsync(today);

        Assert.Equal(300, income);
        Assert.Equal(80, expense);
        Assert.Equal(0, adj);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectMovement()
    {
        var (svc, _) = Create();

        var (created, _) = await svc.CreateAsync(new CashMovement
        {
            TenantId = TenantId,
            MovementType = CashMovementType.Income,
            Amount = 75,
            Description = "Prueba",
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = "u"
        });

        var found = await svc.GetByIdAsync(created!.Id);

        Assert.NotNull(found);
        Assert.Equal(75, found!.Amount);
        Assert.Equal("Prueba", found.Description);
    }
}
