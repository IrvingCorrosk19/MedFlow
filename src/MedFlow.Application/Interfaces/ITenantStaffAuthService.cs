namespace MedFlow.Application.Interfaces;

/// <summary>
/// Login por API (JWT) para usuarios de clínica: Admin, Reception, Doctor, Billing, Staff.
/// No incluye Patient (usa <see cref="IMobileAuthService"/> / portal paciente).
/// </summary>
public interface ITenantStaffAuthService
{
    Task<StaffJwtLoginResult?> LoginAsync(StaffJwtLoginRequest request, CancellationToken cancellationToken = default);
}

public sealed record StaffJwtLoginRequest(
    string Email,
    string Password,
    string TenantCode);

public sealed record StaffJwtLoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    StaffJwtUserInfo User);

public sealed record StaffJwtUserInfo(
    string UserId,
    string Email,
    string? FullName,
    Guid TenantId,
    string TenantName,
    IReadOnlyList<string> Roles);
