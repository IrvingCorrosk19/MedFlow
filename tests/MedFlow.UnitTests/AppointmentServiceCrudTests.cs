using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

public class AppointmentServiceCrudTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (AppointmentService svc,
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Mock<ISubscriptionLimitService> limits,
        Mock<IEventLogService> eventLog,
        Mock<IAuditLogService> audit) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        var limits = new Mock<ISubscriptionLimitService>();
        var eventLog = new Mock<IEventLogService>();
        var audit = new Mock<IAuditLogService>();
        var notifications = new Mock<INotificationDispatchService>();
        notifications.Setup(n => n.DispatchAsync(It.IsAny<MedFlow.Application.Notifications.DispatchRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new MedFlow.Application.Interfaces.DispatchResult(true, Array.Empty<Guid>(), Array.Empty<string>()));
        return (new AppointmentService(db, tenant.Object, limits.Object, eventLog.Object, audit.Object, notifications.Object), db, limits, eventLog, audit);
    }

    private static void AllowCreate(Mock<ISubscriptionLimitService> limits) =>
        limits.Setup(l => l.CanCreateAppointmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(true, null, null));

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Succeeds_WhenNoConflictAndLimitAllows()
    {
        var (svc, db, limits, _, _) = Create();
        AllowCreate(limits);

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id,
            start: TimeSpan.FromHours(9), end: TimeSpan.FromHours(10));
        var (success, error) = await svc.CreateAsync(apt);

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenConflictExists()
    {
        var (svc, db, limits, _, _) = Create();
        AllowCreate(limits);

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);

        // Existing appointment at 9-10
        db.Appointments.Add(SeedData.Appointment(TenantId, doctor.Id, patient.Id,
            start: TimeSpan.FromHours(9), end: TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        // Try to create overlapping 9:30-10:30
        var conflicting = SeedData.Appointment(TenantId, doctor.Id, patient.Id,
            start: TimeSpan.FromHours(9.5), end: TimeSpan.FromHours(10.5));
        var (success, error) = await svc.CreateAsync(conflicting);

        Assert.False(success);
        Assert.Contains("horario", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenPlanLimitReached()
    {
        var (svc, _, limits, _, _) = Create();
        limits.Setup(l => l.CanCreateAppointmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new LimitCheckResult(false, "Límite mensual alcanzado.", null));

        var apt = SeedData.Appointment(TenantId, Guid.NewGuid(), Guid.NewGuid());
        var (success, error) = await svc.CreateAsync(apt);

        Assert.False(success);
        Assert.Contains("Límite", error);
    }

    [Fact]
    public async Task CreateAsync_EnqueuesAppointmentCreatedEvent()
    {
        var (svc, db, limits, eventLog, _) = Create();
        AllowCreate(limits);

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id);
        await svc.CreateAsync(apt);

        eventLog.Verify(e => e.EnqueueAsync(
            "AppointmentCreated", It.IsAny<object>(), "Appointment",
            apt.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Succeeds_WhenNoConflict()
    {
        var (svc, db, _, _, _) = Create();

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id,
            start: TimeSpan.FromHours(9), end: TimeSpan.FromHours(10));
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        apt.Reason = "Seguimiento";
        var (success, error) = await svc.UpdateAsync(apt);

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task UpdateAsync_EnqueuesConfirmedEvent_WhenStatusChangesToConfirmed()
    {
        var (svc, db, _, eventLog, _) = Create();

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id,
            status: AppointmentStatus.Scheduled);
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        apt.Status = AppointmentStatus.Confirmed;
        await svc.UpdateAsync(apt);

        eventLog.Verify(e => e.EnqueueAsync(
            "AppointmentConfirmed", It.IsAny<object>(), "Appointment",
            apt.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EnqueuesCancelledEvent_WhenStatusChangesToCancelled()
    {
        var (svc, db, _, eventLog, _) = Create();

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id);
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        apt.Status = AppointmentStatus.Cancelled;
        await svc.UpdateAsync(apt);

        eventLog.Verify(e => e.EnqueueAsync(
            "AppointmentCancelled", It.IsAny<object>(), "Appointment",
            apt.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletesAppointment()
    {
        var (svc, db, _, _, _) = Create();

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        var apt = SeedData.Appointment(TenantId, doctor.Id, patient.Id);
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await svc.DeleteAsync(apt.Id);

        var raw = await db.Appointments.FindAsync(apt.Id);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_IsNoOp_WhenAppointmentNotFound()
    {
        var (svc, _, _, _, _) = Create();
        // Should not throw
        await svc.DeleteAsync(Guid.NewGuid());
    }

    // ── Get by date ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByDateAsync_ReturnsOnly_AppointmentsOnThatDay()
    {
        var (svc, db, _, _, _) = Create();

        var doctor = SeedData.Doctor(TenantId);
        var patient = SeedData.Patient(TenantId);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        db.Appointments.Add(SeedData.Appointment(TenantId, doctor.Id, patient.Id, date: today));
        db.Appointments.Add(SeedData.Appointment(TenantId, doctor.Id, patient.Id, date: tomorrow,
            start: TimeSpan.FromHours(11), end: TimeSpan.FromHours(12)));
        await db.SaveChangesAsync();

        var result = await svc.GetByDateAsync(today);

        Assert.Single(result);
        Assert.Equal(today, result[0].ScheduledDate);
    }
}
