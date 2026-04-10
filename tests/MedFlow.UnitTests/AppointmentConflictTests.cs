using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Moq;

namespace MedFlow.UnitTests;

public class AppointmentConflictTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();
    private static readonly DateTime Today = DateTime.Today;

    private static AppointmentService CreateService(out MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        db = DbContextFactory.Create(TenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        var limits = new Mock<ISubscriptionLimitService>();
        var eventLog = new Mock<IEventLogService>();
        var audit = new Mock<IAuditLogService>();
        var notifications = new Mock<INotificationDispatchService>();
        notifications.Setup(n => n.DispatchAsync(It.IsAny<MedFlow.Application.Notifications.DispatchRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new MedFlow.Application.Interfaces.DispatchResult(true, Array.Empty<Guid>(), Array.Empty<string>()));
        return new AppointmentService(db, tenant.Object, limits.Object, eventLog.Object, audit.Object, notifications.Object);
    }

    private static Appointment MakeAppointment(TimeSpan start, TimeSpan end, AppointmentStatus status = AppointmentStatus.Scheduled)
        => new()
        {
            Id = Guid.NewGuid(),
            DoctorId = DoctorId,
            PatientId = Guid.NewGuid(),
            TenantId = TenantId,
            ScheduledDate = Today,
            StartTime = start,
            EndTime = end,
            Status = status
        };

    // ── Overlap scenarios ──────────────────────────────────────────────────

    [Fact]
    public async Task HasConflict_ReturnsFalse_WhenNoAppointmentsExist()
    {
        var svc = CreateService(out _);
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsTrue_WhenExactSameSlot()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.True(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsTrue_WhenNewSlotStartsInsideExisting()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        // 9:30–10:30 overlaps with 9:00–10:00
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9.5), TimeSpan.FromHours(10.5));
        Assert.True(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsTrue_WhenNewSlotEndsInsideExisting()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        // 8:30–9:30 overlaps with 9:00–10:00
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(8.5), TimeSpan.FromHours(9.5));
        Assert.True(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_WhenNewSlotImmediatelyAfter()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        // 10:00–11:00 starts exactly when previous ends — no overlap
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(10), TimeSpan.FromHours(11));
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_WhenNewSlotImmediatelyBefore()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(10), TimeSpan.FromHours(11)));
        await db.SaveChangesAsync();

        // 9:00–10:00 ends exactly when next starts — no overlap
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_ForCancelledAppointments()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10), AppointmentStatus.Cancelled));
        await db.SaveChangesAsync();

        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_WhenExcludedIdMatchesConflictingAppointment()
    {
        var svc = CreateService(out var db);
        var existing = MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        db.Appointments.Add(existing);
        await db.SaveChangesAsync();

        // Same slot but excluded (edit of this appointment)
        var conflict = await svc.HasConflictAsync(DoctorId, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10), excludeId: existing.Id);
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_ForDifferentDoctor()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        var otherDoctor = Guid.NewGuid();
        var conflict = await svc.HasConflictAsync(otherDoctor, Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.False(conflict);
    }

    [Fact]
    public async Task HasConflict_ReturnsFalse_ForDifferentDay()
    {
        var svc = CreateService(out var db);
        db.Appointments.Add(MakeAppointment(TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        await db.SaveChangesAsync();

        var conflict = await svc.HasConflictAsync(DoctorId, Today.AddDays(1), TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.False(conflict);
    }
}
