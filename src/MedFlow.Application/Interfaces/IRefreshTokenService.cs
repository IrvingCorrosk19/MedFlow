namespace MedFlow.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult?> CreateAsync(string userId, string? deviceInfo, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string userId, string? refreshToken = null, CancellationToken cancellationToken = default);
}

public record RefreshTokenResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);
