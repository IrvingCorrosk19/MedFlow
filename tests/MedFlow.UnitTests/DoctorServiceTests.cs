using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

public class DoctorServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (DoctorService svc,
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Mock<ISubscriptionLimitService> limits) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        var limits = new Mock<ISubscriptionLimitService>();
        return (new DoctorService(db, tenant.Object, limits.Object), db, limits);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsDoctor_WhenLimitAllows()
    {
        var (svc, _, limits) = Create();
        limits.Setup(l => l.CanCreateDoctorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(true, null, null));

        var doctor = SeedData.Doctor(TenantId);
        var (result, error) = await svc.CreateAsync(doctor);

        Assert.NotNull(result);
        Assert.Null(error);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenDoctorLimitReached()
    {
        var (svc, _, limits) = Create();
        limits.Setup(l => l.CanCreateDoctorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(false, "Ha alcanzado el límite de doctores.", null));

        var (result, error) = await svc.CreateAsync(SeedData.Doctor(TenantId));

        Assert.Null(result);
        Assert.Contains("doctores", error);
    }

    // ── Read ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsDoctor_WhenExists()
    {
        var (svc, db, _) = Create();
        var doctor = SeedData.Doctor(TenantId, "Ana", "Morales");
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var result = await svc.GetByIdAsync(doctor.Id);

        Assert.NotNull(result);
        Assert.Equal("Morales", result!.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var (svc, _, _) = Create();
        var result = await svc.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveDoctors_WhenFiltered()
    {
        var (svc, db, _) = Create();
        db.Doctors.Add(SeedData.Doctor(TenantId, "Active", "One"));
        var inactive = SeedData.Doctor(TenantId, "Inactive", "Two");
        inactive.IsActive = false;
        db.Doctors.Add(inactive);
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(isActive: true);

        Assert.Single(result);
        Assert.Equal("One", result[0].LastName);
    }

    [Fact]
    public async Task GetAllAsync_SearchBySpecialty()
    {
        var (svc, db, _) = Create();
        db.Doctors.Add(SeedData.Doctor(TenantId, "Maria", "R", specialty: "Cardiologia"));
        db.Doctors.Add(SeedData.Doctor(TenantId, "Pedro", "T", specialty: "Pediatria"));
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(search: "cardio");

        Assert.Single(result);
        Assert.Equal("Cardiologia", result[0].Speciality);
    }

    [Fact]
    public async Task GetAllAsync_IsSortedByLastNameThenFirstName()
    {
        var (svc, db, _) = Create();
        db.Doctors.Add(SeedData.Doctor(TenantId, "Zara", "Morales"));
        db.Doctors.Add(SeedData.Doctor(TenantId, "Ana", "Lopez"));
        db.Doctors.Add(SeedData.Doctor(TenantId, "Carlos", "Lopez"));
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync();

        Assert.Equal("Lopez", result[0].LastName);
        Assert.Equal("Ana", result[0].FirstName);
        Assert.Equal("Lopez", result[1].LastName);
        Assert.Equal("Carlos", result[1].FirstName);
        Assert.Equal("Morales", result[2].LastName);
    }

    // ── Paged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsTotalCount_AndPaginatedItems()
    {
        var (svc, db, _) = Create();
        for (var i = 1; i <= 12; i++)
            db.Doctors.Add(SeedData.Doctor(TenantId, $"Dr{i:D2}", "X"));
        await db.SaveChangesAsync();

        var page1 = await svc.GetPagedAsync(page: 1, pageSize: 5);
        var page2 = await svc.GetPagedAsync(page: 2, pageSize: 5);

        Assert.Equal(12, page1.TotalCount);
        Assert.Equal(5, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Empty(page1.Items.Select(d => d.Id).Intersect(page2.Items.Select(d => d.Id)));
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var (svc, db, _) = Create();
        var doctor = SeedData.Doctor(TenantId);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        doctor.Speciality = "Neurologia";
        var before = DateTime.UtcNow.AddSeconds(-1);
        var updated = await svc.UpdateAsync(doctor);

        Assert.Equal("Neurologia", updated.Speciality);
        Assert.True(updated.UpdatedAt >= before);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletesDoctor()
    {
        var (svc, db, _) = Create();
        var doctor = SeedData.Doctor(TenantId);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await svc.DeleteAsync(doctor.Id);

        var result = await svc.GetByIdAsync(doctor.Id);
        Assert.Null(result); // filtered out by IsDeleted

        var raw = await db.Doctors.FindAsync(doctor.Id);
        Assert.True(raw!.IsDeleted);
        Assert.False(raw.IsActive);
    }
}
