using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;

namespace MedFlow.UnitTests;

public class SubscriptionLimitServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (SubscriptionLimitService svc,
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db) Create()
    {
        // SubscriptionLimitService uses IgnoreQueryFilters internally, so we use the admin context
        var db = DbContextFactory.CreateWithIgnoredFilter();
        return (new SubscriptionLimitService(db), db);
    }

    // ── No active plan ─────────────────────────────────────────────────────

    [Fact]
    public async Task CanCreatePatient_ReturnsDeny_WhenNoPlanExists()
    {
        var (svc, _) = Create();
        var result = await svc.CanCreatePatientAsync(TenantId);
        Assert.False(result.Allowed);
        Assert.Contains("plan", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanCreateDoctor_ReturnsDeny_WhenNoPlanExists()
    {
        var (svc, _) = Create();
        var result = await svc.CanCreateDoctorAsync(TenantId);
        Assert.False(result.Allowed);
    }

    // ── Within limits ──────────────────────────────────────────────────────

    [Fact]
    public async Task CanCreatePatient_ReturnsAllow_WhenUnderLimit()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxPatients: 10);

        var result = await svc.CanCreatePatientAsync(TenantId);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task CanCreateDoctor_ReturnsAllow_WhenUnderLimit()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxDoctors: 5);

        var result = await svc.CanCreateDoctorAsync(TenantId);

        Assert.True(result.Allowed);
    }

    // ── At limit ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CanCreatePatient_ReturnsDeny_WhenAtLimit()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxPatients: 2);

        // Seed 2 patients (the limit)
        db.Patients.AddRange(
            SeedData.Patient(TenantId),
            SeedData.Patient(TenantId));
        await db.SaveChangesAsync();

        var result = await svc.CanCreatePatientAsync(TenantId);

        Assert.False(result.Allowed);
        Assert.Contains("2", result.Message); // message includes the plan limit
        Assert.NotNull(result.Suggestion);
    }

    [Fact]
    public async Task CanCreateDoctor_ReturnsDeny_WhenAtLimit()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxDoctors: 1);

        db.Doctors.Add(SeedData.Doctor(TenantId));
        await db.SaveChangesAsync();

        var result = await svc.CanCreateDoctorAsync(TenantId);

        Assert.False(result.Allowed);
        Assert.Contains("1", result.Message);
    }

    // ── Unlimited plan (MaxX <= 0) ─────────────────────────────────────────

    [Fact]
    public async Task CanCreatePatient_ReturnsAllow_WhenPlanIsUnlimited()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxPatients: 0); // 0 = unlimited

        // Seed many patients
        for (var i = 0; i < 50; i++)
            db.Patients.Add(SeedData.Patient(TenantId));
        await db.SaveChangesAsync();

        var result = await svc.CanCreatePatientAsync(TenantId);

        Assert.True(result.Allowed);
    }

    // ── Monthly appointment limit ──────────────────────────────────────────

    [Fact]
    public async Task CanCreateAppointment_ReturnsDeny_WhenMonthlyLimitReached()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxAppointmentsPerMonth: 2);

        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 0, 0, 0, DateTimeKind.Utc);

        db.Appointments.AddRange(
            SeedData.Appointment(TenantId, doctorId, patientId, date: thisMonth),
            SeedData.Appointment(TenantId, doctorId, patientId, date: thisMonth,
                start: TimeSpan.FromHours(11), end: TimeSpan.FromHours(12)));
        await db.SaveChangesAsync();

        var result = await svc.CanCreateAppointmentAsync(TenantId);

        Assert.False(result.Allowed);
        Assert.Contains("2", result.Message);
    }

    [Fact]
    public async Task CanCreateAppointment_ReturnsAllow_WhenPastMonthAppointmentsDontCount()
    {
        var (svc, db) = Create();
        await SeedData.SeedSubscriptionAsync(db, TenantId, maxAppointmentsPerMonth: 1);

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        db.Appointments.Add(SeedData.Appointment(TenantId, Guid.NewGuid(), Guid.NewGuid(), date: lastMonth));
        await db.SaveChangesAsync();

        // Last month's appointment should NOT count against this month's limit
        var result = await svc.CanCreateAppointmentAsync(TenantId);

        Assert.True(result.Allowed);
    }

    // ── Plan status ────────────────────────────────────────────────────────

    [Fact]
    public async Task CanCreatePatient_ReturnsDeny_WhenSubscriptionIsCancelled()
    {
        var (svc, db) = Create();
        var plan = SeedData.Plan(maxPatients: 100);
        var sub = SeedData.ActiveSubscription(TenantId, plan.Id);
        sub.Status = MedFlow.Domain.Enums.SubscriptionStatus.Cancelled;
        db.SubscriptionPlans.Add(plan);
        db.TenantSubscriptions.Add(sub);
        await db.SaveChangesAsync();

        var result = await svc.CanCreatePatientAsync(TenantId);

        Assert.False(result.Allowed); // Cancelled subscriptions are not considered active
    }
}
