namespace MedFlow.Application.Interfaces;

public interface IPushDeviceTokenService
{
    Task RegisterAsync(RegisterPushTokenRequest request, CancellationToken cancellationToken = default);
    Task UnregisterAsync(Guid tenantId, string userId, string token, CancellationToken cancellationToken = default);
}

public record RegisterPushTokenRequest(
    Guid TenantId,
    string UserId,
    string? PatientId,
    string Token,
    string Platform,
    string? DeviceId);
