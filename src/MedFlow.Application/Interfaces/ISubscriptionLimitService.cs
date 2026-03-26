using MedFlow.Application.Saas;

namespace MedFlow.Application.Interfaces;

public interface ISubscriptionLimitService
{
    Task<LimitCheckResult> CanCreateUserAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LimitCheckResult> CanCreateDoctorAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LimitCheckResult> CanCreatePatientAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LimitCheckResult> CanCreateAppointmentAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantUsageDto> GetCurrentUsageAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PlanLimitsDto?> GetPlanLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record LimitCheckResult(bool Allowed, string? Message, string? Suggestion);
