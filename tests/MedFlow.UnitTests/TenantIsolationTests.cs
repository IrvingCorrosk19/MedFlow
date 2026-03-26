using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Verifies that the multi-tenant query filters correctly isolate data between tenants.
/// A tenant must NEVER see data belonging to a different tenant.
/// These are the most critical tests in the suite.
/// </summary>
public class TenantIsolationTests
{
    // ── Patient isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task Patient_TenantA_CannotSee_TenantB_Patients()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        // Seed one patient per tenant in the SAME database
        ctxA.Patients.Add(SeedData.Patient(tidA, "Garcia"));
        ctxB.Patients.Add(SeedData.Patient(tidB, "Lopez"));
        await ctxA.SaveChangesAsync();
        await ctxB.SaveChangesAsync();

        var svcA = PatientSvc(ctxA, tidA);
        var svcB = PatientSvc(ctxB, tidB);

        var patientsSeenByA = await svcA.GetAllAsync();
        var patientsSeenByB = await svcB.GetAllAsync();

        Assert.Single(patientsSeenByA);
        Assert.Equal("Garcia", patientsSeenByA[0].PrimerApellido);

        Assert.Single(patientsSeenByB);
        Assert.Equal("Lopez", patientsSeenByB[0].PrimerApellido);
    }

    [Fact]
    public async Task Patient_GetById_TenantA_CannotFetch_TenantB_Patient()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var patientB = SeedData.Patient(tidB, "Lopez");
        ctxB.Patients.Add(patientB);
        await ctxB.SaveChangesAsync();

        // Tenant A tries to read Tenant B's patient by its known ID
        var svcA = PatientSvc(ctxA, tidA);
        var result = await svcA.GetByIdAsync(patientB.Id);

        Assert.Null(result); // Must NOT be found
    }

    [Fact]
    public async Task Patient_SoftDeleted_IsNotReturnedToAnyTenant()
    {
        var (ctxA, _) = DbContextFactory.CreateTwentyTenants(out var tidA, out _);

        var patient = SeedData.Patient(tidA, "Deleted");
        patient.IsDeleted = true;
        ctxA.Patients.Add(patient);
        await ctxA.SaveChangesAsync();

        var svc = PatientSvc(ctxA, tidA);
        var all = await svc.GetAllAsync();

        Assert.Empty(all);
    }

    // ── Doctor isolation ───────────────────────────────────────────────────

    [Fact]
    public async Task Doctor_TenantA_CannotSee_TenantB_Doctors()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        ctxA.Doctors.Add(SeedData.Doctor(tidA, "Maria", "Ramirez"));
        ctxB.Doctors.Add(SeedData.Doctor(tidB, "Pedro", "Torres"));
        await ctxA.SaveChangesAsync();
        await ctxB.SaveChangesAsync();

        var svcA = DoctorSvc(ctxA, tidA);
        var svcB = DoctorSvc(ctxB, tidB);

        var docsByA = await svcA.GetAllAsync();
        var docsByB = await svcB.GetAllAsync();

        Assert.Single(docsByA);
        Assert.Equal("Ramirez", docsByA[0].LastName);

        Assert.Single(docsByB);
        Assert.Equal("Torres", docsByB[0].LastName);
    }

    [Fact]
    public async Task Doctor_GetById_TenantA_CannotFetch_TenantB_Doctor()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var doctorB = SeedData.Doctor(tidB, "Pedro", "Torres");
        ctxB.Doctors.Add(doctorB);
        await ctxB.SaveChangesAsync();

        var svcA = DoctorSvc(ctxA, tidA);
        var result = await svcA.GetByIdAsync(doctorB.Id);

        Assert.Null(result);
    }

    // ── Appointment isolation ──────────────────────────────────────────────

    [Fact]
    public async Task Appointment_TenantA_CannotSee_TenantB_Appointments()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var doctorA = SeedData.Doctor(tidA);
        var patientA = SeedData.Patient(tidA);
        var doctorB = SeedData.Doctor(tidB);
        var patientB = SeedData.Patient(tidB);

        ctxA.Doctors.Add(doctorA);
        ctxA.Patients.Add(patientA);
        ctxA.Appointments.Add(SeedData.Appointment(tidA, doctorA.Id, patientA.Id));
        await ctxA.SaveChangesAsync();

        ctxB.Doctors.Add(doctorB);
        ctxB.Patients.Add(patientB);
        ctxB.Appointments.Add(SeedData.Appointment(tidB, doctorB.Id, patientB.Id));
        await ctxB.SaveChangesAsync();

        var svcA = AppointmentSvc(ctxA, tidA);
        var svcB = AppointmentSvc(ctxB, tidB);

        var aptsA = await svcA.GetAllAsync();
        var aptsB = await svcB.GetAllAsync();

        Assert.Single(aptsA);
        Assert.Single(aptsB);

        // No cross-tenant leakage
        Assert.DoesNotContain(aptsA, a => a.TenantId == tidB);
        Assert.DoesNotContain(aptsB, a => a.TenantId == tidA);
    }

    [Fact]
    public async Task Appointment_GetById_TenantA_CannotFetch_TenantB_Appointment()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var doctorB = SeedData.Doctor(tidB);
        var patientB = SeedData.Patient(tidB);
        var aptB = SeedData.Appointment(tidB, doctorB.Id, patientB.Id);

        ctxB.Doctors.Add(doctorB);
        ctxB.Patients.Add(patientB);
        ctxB.Appointments.Add(aptB);
        await ctxB.SaveChangesAsync();

        var svcA = AppointmentSvc(ctxA, tidA);
        var result = await svcA.GetByIdAsync(aptB.Id);

        Assert.Null(result);
    }

    // ── Delete cannot cross tenant ─────────────────────────────────────────

    [Fact]
    public async Task Patient_DeleteAsync_TenantA_CannotDelete_TenantB_Patient()
    {
        var (ctxA, ctxB) = DbContextFactory.CreateTwentyTenants(out var tidA, out var tidB);

        var patientB = SeedData.Patient(tidB, "Lopez");
        ctxB.Patients.Add(patientB);
        await ctxB.SaveChangesAsync();

        var svcA = PatientSvc(ctxA, tidA);

        // Tenant A attempts to delete Tenant B's patient
        await svcA.DeleteAsync(patientB.Id); // Must be a no-op

        // Verify patient B still exists and is NOT deleted
        var svcB = PatientSvc(ctxB, tidB);
        var still = await svcB.GetByIdAsync(patientB.Id);
        Assert.NotNull(still);
        Assert.False(still!.IsDeleted);
    }

    // ── Factory helpers ────────────────────────────────────────────────────

    private static PatientService PatientSvc(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var tenant = Tenant(tenantId);
        return new PatientService(db, tenant, new Mock<ISubscriptionLimitService>().Object,
            new Mock<IEventLogService>().Object, new Mock<IAuditLogService>().Object);
    }

    private static DoctorService DoctorSvc(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var tenant = Tenant(tenantId);
        return new DoctorService(db, tenant, new Mock<ISubscriptionLimitService>().Object);
    }

    private static AppointmentService AppointmentSvc(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var tenant = Tenant(tenantId);
        return new AppointmentService(db, tenant, new Mock<ISubscriptionLimitService>().Object,
            new Mock<IEventLogService>().Object, new Mock<IAuditLogService>().Object);
    }

    private static ITenantContext Tenant(Guid tenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(t => t.TenantId).Returns(tenantId);
        mock.Setup(t => t.IgnoreTenantFilter).Returns(false);
        return mock.Object;
    }
}
