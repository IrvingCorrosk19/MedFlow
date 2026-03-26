using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;

namespace MedFlow.UnitTests.Helpers;

/// <summary>Centralized seed helpers shared across test classes.</summary>
internal static class SeedData
{
    public static Patient Patient(Guid tenantId, string apellido = "Garcia", string nombre = "Ana", bool active = true)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PrimerNombre = nombre,
            PrimerApellido = apellido,
            IsActive = active,
            NumeroDocumento = $"DOC-{Guid.NewGuid():N}"[..12]
        };

    public static Doctor Doctor(Guid tenantId, string firstName = "Carlos", string lastName = "Ruiz", string? specialty = "General")
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Speciality = specialty,
            IsActive = true
        };

    public static Appointment Appointment(Guid tenantId, Guid doctorId, Guid patientId,
        DateTime? date = null, TimeSpan? start = null, TimeSpan? end = null,
        AppointmentStatus status = AppointmentStatus.Scheduled)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DoctorId = doctorId,
            PatientId = patientId,
            ScheduledDate = date ?? DateTime.Today,
            StartTime = start ?? TimeSpan.FromHours(9),
            EndTime = end ?? TimeSpan.FromHours(10),
            Status = status
        };

    public static SubscriptionPlan Plan(int maxPatients = 100, int maxDoctors = 5,
        int maxUsers = 10, int maxAppointmentsPerMonth = 500)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Plan",
            Code = $"TEST-{Guid.NewGuid():N}"[..10],
            MaxPatients = maxPatients,
            MaxDoctors = maxDoctors,
            MaxUsers = maxUsers,
            MaxAppointmentsPerMonth = maxAppointmentsPerMonth
        };

    public static TenantSubscription ActiveSubscription(Guid tenantId, Guid planId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionPlanId = planId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };

    public static async Task SeedSubscriptionAsync(ApplicationDbContext db, Guid tenantId,
        int maxPatients = 100, int maxDoctors = 5, int maxAppointmentsPerMonth = 500)
    {
        var plan = Plan(maxPatients, maxDoctors, maxAppointmentsPerMonth: maxAppointmentsPerMonth);
        var sub = ActiveSubscription(tenantId, plan.Id);
        db.SubscriptionPlans.Add(plan);
        db.TenantSubscriptions.Add(sub);
        await db.SaveChangesAsync();
    }
}
