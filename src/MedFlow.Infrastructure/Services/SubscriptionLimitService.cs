using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class SubscriptionLimitService : ISubscriptionLimitService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionLimitService(ApplicationDbContext db)
    {
        _db = db;
    }

    private static bool IsUnlimited(int max) => max <= 0;

    public async Task<LimitCheckResult> CanCreateUserAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        if (plan == null)
            return Deny("No hay un plan de suscripción activo para esta clínica.", "Contacte al administrador de la plataforma.");

        var used = await CountUsersAsync(tenantId, cancellationToken);
        if (!IsUnlimited(plan.MaxUsers) && used >= plan.MaxUsers)
            return Deny(
                $"Ha alcanzado el límite de usuarios de su plan ({plan.MaxUsers}).",
                "Actualice su plan para añadir más usuarios.");

        return Ok();
    }

    public async Task<LimitCheckResult> CanCreateDoctorAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        if (plan == null)
            return Deny("No hay un plan de suscripción activo para esta clínica.", "Contacte al administrador de la plataforma.");

        var used = await CountDoctorsAsync(tenantId, cancellationToken);
        if (!IsUnlimited(plan.MaxDoctors) && used >= plan.MaxDoctors)
            return Deny(
                $"Ha alcanzado el límite de doctores de su plan ({plan.MaxDoctors}).",
                "Actualice su plan para registrar más médicos.");

        return Ok();
    }

    public async Task<LimitCheckResult> CanCreatePatientAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        if (plan == null)
            return Deny("No hay un plan de suscripción activo para esta clínica.", "Contacte al administrador de la plataforma.");

        var used = await CountPatientsAsync(tenantId, cancellationToken);
        if (!IsUnlimited(plan.MaxPatients) && used >= plan.MaxPatients)
            return Deny(
                $"Ha alcanzado el límite de pacientes de su plan ({plan.MaxPatients}).",
                "Actualice su plan para registrar más pacientes.");

        return Ok();
    }

    public async Task<LimitCheckResult> CanCreateAppointmentAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        if (plan == null)
            return Deny("No hay un plan de suscripción activo para esta clínica.", "Contacte al administrador de la plataforma.");

        var used = await CountAppointmentsThisMonthAsync(tenantId, cancellationToken);
        if (!IsUnlimited(plan.MaxAppointmentsPerMonth) && used >= plan.MaxAppointmentsPerMonth)
            return Deny(
                $"Ha alcanzado el límite de citas mensuales de su plan ({plan.MaxAppointmentsPerMonth}).",
                "Actualice su plan o espere al próximo mes según su política comercial.");

        return Ok();
    }

    public async Task<TenantUsageDto> GetCurrentUsageAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var limits = await GetPlanLimitsAsync(tenantId, cancellationToken);
        var maxUsers = limits?.MaxUsers ?? 0;
        var maxDoc = limits?.MaxDoctors ?? 0;
        var maxPat = limits?.MaxPatients ?? 0;
        var maxApt = limits?.MaxAppointmentsPerMonth ?? 0;

        return new TenantUsageDto
        {
            Users = await CountUsersAsync(tenantId, cancellationToken),
            Doctors = await CountDoctorsAsync(tenantId, cancellationToken),
            Patients = await CountPatientsAsync(tenantId, cancellationToken),
            AppointmentsThisMonth = await CountAppointmentsThisMonthAsync(tenantId, cancellationToken),
            MaxUsers = maxUsers,
            MaxDoctors = maxDoc,
            MaxPatients = maxPat,
            MaxAppointmentsPerMonth = maxApt,
            IncludesBillingModule = limits?.IncludesBillingModule ?? false,
            IncludesAutomationModule = limits?.IncludesAutomationModule ?? false,
            IncludesReportsModule = limits?.IncludesReportsModule ?? false,
            IncludesPatientPortal = limits?.IncludesPatientPortal ?? false,
            IncludesMultiBranch = limits?.IncludesMultiBranch ?? false,
            IncludesAdvancedAnalytics = limits?.IncludesAdvancedAnalytics ?? false
        };
    }

    public async Task<PlanLimitsDto?> GetPlanLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(tenantId, cancellationToken);
        if (plan == null) return null;

        return new PlanLimitsDto
        {
            MaxUsers = plan.MaxUsers,
            MaxDoctors = plan.MaxDoctors,
            MaxPatients = plan.MaxPatients,
            MaxAppointmentsPerMonth = plan.MaxAppointmentsPerMonth,
            MaxBranches = plan.MaxBranches,
            IncludesBillingModule = plan.IncludesBillingModule,
            IncludesAutomationModule = plan.IncludesAutomationModule,
            IncludesReportsModule = plan.IncludesReportsModule,
            IncludesPatientPortal = plan.IncludesPatientPortal,
            IncludesMultiBranch = plan.IncludesMultiBranch,
            IncludesAdvancedAnalytics = plan.IncludesAdvancedAnalytics
        };
    }

    private Task<SubscriptionPlan?> GetPlanAsync(Guid tenantId, CancellationToken cancellationToken) =>
        TenantSubscriptionPlanHelper.GetEffectivePlanAsync(_db, tenantId, cancellationToken);

    private Task<int> CountUsersAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.Set<ApplicationUser>().IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == tenantId && u.IsActive, cancellationToken);

    private Task<int> CountDoctorsAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.Doctors.IgnoreQueryFilters()
            .CountAsync(d => d.TenantId == tenantId && !d.IsDeleted, cancellationToken);

    private Task<int> CountPatientsAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.Patients.IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == tenantId && !p.IsDeleted, cancellationToken);

    private Task<int> CountAppointmentsThisMonthAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        return _db.Appointments.IgnoreQueryFilters()
            .CountAsync(a => a.TenantId == tenantId && !a.IsDeleted && a.ScheduledDate >= start && a.ScheduledDate < end, cancellationToken);
    }

    private static LimitCheckResult Ok() => new(true, null, null);

    private static LimitCheckResult Deny(string message, string? suggestion) => new(false, message, suggestion);
}
