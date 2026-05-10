using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

public class PatientServicePaginationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (PatientService svc, MedFlow.Infrastructure.Persistence.ApplicationDbContext db) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        var limits = new Mock<ISubscriptionLimitService>();
        var eventLog = new Mock<IEventLogService>();
        var audit = new Mock<IAuditLogService>();
        var svc = new PatientService(db, tenant.Object, MockClinicalUserScope.NoDoctorRestriction(), limits.Object, eventLog.Object, audit.Object);
        return (svc, db);
    }

    private static Patient MakePatient(string primerApellido, string primerNombre, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            PrimerNombre = primerNombre,
            PrimerApellido = primerApellido,
            IsActive = isActive
        };

    private static async Task SeedAsync(MedFlow.Infrastructure.Persistence.ApplicationDbContext db, IEnumerable<Patient> patients)
    {
        db.Patients.AddRange(patients);
        await db.SaveChangesAsync();
    }

    // ── Page size clamping ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ClampsPageSizeToMinimumOne()
    {
        var (svc, db) = Create();
        await SeedAsync(db, [MakePatient("Garcia", "Ana")]);

        var result = await svc.GetAllAsync(pageSize: -5);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_ClampsPageSizeToMaximum500()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 10).Select(i => MakePatient($"Apellido{i}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var result = await svc.GetAllAsync(pageSize: 99999);
        Assert.Equal(10, result.Count); // all 10 returned, clamped to 500 but only 10 exist
    }

    [Fact]
    public async Task GetAllAsync_RespectsPageSizeLimit()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 10).Select(i => MakePatient($"Apellido{i}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var result = await svc.GetAllAsync(pageSize: 3);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_SecondPageReturnsDifferentRecords()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 6).Select(i => MakePatient($"Apellido{i:D2}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var page1 = await svc.GetAllAsync(page: 1, pageSize: 3);
        var page2 = await svc.GetAllAsync(page: 2, pageSize: 3);

        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        Assert.Empty(page1.Select(p => p.Id).Intersect(page2.Select(p => p.Id)));
    }

    // ── Active filter ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByActiveStatus()
    {
        var (svc, db) = Create();
        await SeedAsync(db, [
            MakePatient("Garcia", "Ana", isActive: true),
            MakePatient("Lopez", "Juan", isActive: false)
        ]);

        var active = await svc.GetAllAsync(isActive: true);
        Assert.Single(active);
        Assert.Equal("Garcia", active[0].PrimerApellido);
    }

    // ── Search ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_SearchByLastName_CaseInsensitive()
    {
        var (svc, db) = Create();
        await SeedAsync(db, [
            MakePatient("Martinez", "Carlos"),
            MakePatient("Rodriguez", "Maria")
        ]);

        var result = await svc.GetAllAsync(search: "MARTINEZ");
        Assert.Single(result);
        Assert.Equal("Martinez", result[0].PrimerApellido);
    }

    // ── GetPagedAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsTotalCount()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 15).Select(i => MakePatient($"Apellido{i:D2}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var result = await svc.GetPagedAsync(page: 1, pageSize: 5);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_HasNextPageAndPreviousPage_AreAccurate()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 10).Select(i => MakePatient($"Apellido{i:D2}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var page2 = await svc.GetPagedAsync(page: 2, pageSize: 4);

        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_HasNoNextPage()
    {
        var (svc, db) = Create();
        var patients = Enumerable.Range(1, 8).Select(i => MakePatient($"Apellido{i:D2}", "Juan")).ToList();
        await SeedAsync(db, patients);

        var lastPage = await svc.GetPagedAsync(page: 2, pageSize: 4);

        Assert.False(lastPage.HasNextPage);
        Assert.Equal(8, lastPage.TotalCount);
    }
}
