using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

public class PatientServiceCrudTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (PatientService svc,
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Mock<ISubscriptionLimitService> limits,
        Mock<IAuditLogService> audit,
        Mock<IEventLogService> eventLog) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        var limits = new Mock<ISubscriptionLimitService>();
        var eventLog = new Mock<IEventLogService>();
        var audit = new Mock<IAuditLogService>();
        var svc = new PatientService(db, tenant.Object, MockClinicalUserScope.NoDoctorRestriction(), limits.Object, eventLog.Object, audit.Object);
        return (svc, db, limits, audit, eventLog);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsPatient_WhenLimitAllows()
    {
        var (svc, _, limits, _, _) = Create();
        limits.Setup(l => l.CanCreatePatientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(true, null, null));

        var patient = SeedData.Patient(TenantId);
        var (result, error) = await svc.CreateAsync(patient);

        Assert.NotNull(result);
        Assert.Null(error);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenPlanLimitReached()
    {
        var (svc, _, limits, _, _) = Create();
        limits.Setup(l => l.CanCreatePatientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(false, "Ha alcanzado el límite.", "Actualice su plan."));

        var patient = SeedData.Patient(TenantId);
        var (result, error) = await svc.CreateAsync(patient);

        Assert.Null(result);
        Assert.Contains("límite", error);
    }

    [Fact]
    public async Task CreateAsync_EnqueuesEvent_WithOnlyPatientId()
    {
        var (svc, _, limits, _, eventLog) = Create();
        limits.Setup(l => l.CanCreatePatientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(true, null, null));

        var patient = SeedData.Patient(TenantId);
        patient.Correo = "ana.garcia@email.com";
        patient.Telefono = "555-1234";

        await svc.CreateAsync(patient);

        // Verify event was enqueued and payload does NOT contain PII fields
        eventLog.Verify(e => e.EnqueueAsync(
            "PatientCreated",
            It.Is<object>(payload =>
                !payload.ToString()!.Contains("Correo") &&
                !payload.ToString()!.Contains("Telefono") &&
                !payload.ToString()!.Contains("NumeroDocumento")),
            "Patient",
            patient.Id.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WritesAuditLog()
    {
        var (svc, _, limits, audit, _) = Create();
        limits.Setup(l => l.CanCreatePatientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(true, null, null));

        var patient = SeedData.Patient(TenantId);
        await svc.CreateAsync(patient);

        audit.Verify(a => a.LogAsync(
            It.Is<AuditLogWriteDto>(d => d.Action == "Create" && d.EntityName == "Patient"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt_AndPersists()
    {
        var (svc, db, _, _, _) = Create();

        var patient = SeedData.Patient(TenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        patient.PrimerNombre = "Maria";
        var before = DateTime.UtcNow.AddSeconds(-1);
        var updated = await svc.UpdateAsync(patient);

        Assert.Equal("Maria", updated.PrimerNombre);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_WritesAuditLog()
    {
        var (svc, db, _, audit, _) = Create();

        var patient = SeedData.Patient(TenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        await svc.UpdateAsync(patient);

        audit.Verify(a => a.LogAsync(
            It.Is<AuditLogWriteDto>(d => d.Action == "Update" && d.EntityName == "Patient"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Delete (soft) ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletesPatient()
    {
        var (svc, db, _, _, _) = Create();

        var patient = SeedData.Patient(TenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); // detach so service can re-attach without conflict

        await svc.DeleteAsync(patient.Id);

        // GetByIdAsync uses tenant filter + !IsDeleted — should return null after soft delete
        var result = await svc.GetByIdAsync(patient.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_SetsIsDeletedAndIsActive()
    {
        var (svc, db, _, _, _) = Create();

        var patient = SeedData.Patient(TenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await svc.DeleteAsync(patient.Id);

        // Read raw (bypass filter) to confirm the entity state
        var raw = await db.Patients.FindAsync(patient.Id);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
        Assert.False(raw.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_IsNoOp_WhenPatientNotFound()
    {
        var (svc, _, _, audit, _) = Create();

        // Should not throw and should not call audit
        await svc.DeleteAsync(Guid.NewGuid());
        audit.Verify(a => a.LogAsync(It.IsAny<AuditLogWriteDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WritesAuditLog_WhenPatientExists()
    {
        var (svc, db, _, audit, _) = Create();

        var patient = SeedData.Patient(TenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await svc.DeleteAsync(patient.Id);

        audit.Verify(a => a.LogAsync(
            It.Is<AuditLogWriteDto>(d => d.Action == "Delete" && d.EntityName == "Patient"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
