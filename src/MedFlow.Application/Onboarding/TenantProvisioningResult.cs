namespace MedFlow.Application.Onboarding;

public sealed class TenantProvisioningResult
{
    public bool Success { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantCode { get; init; }
    public string? AdminUserId { get; init; }
    public bool StartedWithTrial { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static TenantProvisioningResult Ok(Guid tenantId, string tenantCode, string adminUserId, bool startedWithTrial) =>
        new()
        {
            Success = true,
            TenantId = tenantId,
            TenantCode = tenantCode,
            AdminUserId = adminUserId,
            StartedWithTrial = startedWithTrial
        };

    public static TenantProvisioningResult Fail(IReadOnlyList<string> errors) =>
        new() { Success = false, Errors = errors };

    public static TenantProvisioningResult Fail(string error) =>
        new() { Success = false, Errors = [error] };
}
