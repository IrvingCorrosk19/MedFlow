namespace MedFlow.Application.Interfaces;

public interface IMobileAuthService
{
    Task<MobileLoginResult?> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default);
    Task<MobileLoginResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string userId, string? refreshToken, CancellationToken cancellationToken = default);
}

public record MobileLoginRequest(
    string Email,
    string Password,
    Guid? TenantId = null,
    string? TenantCode = null,
    string? DeviceInfo = null);

public record MobileLoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    MobileUserInfo User,
    Guid PatientId);

public record MobileUserInfo(
    string UserId,
    string Email,
    string? FullName,
    Guid TenantId,
    string TenantName);
