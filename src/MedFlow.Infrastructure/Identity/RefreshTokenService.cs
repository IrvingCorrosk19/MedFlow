using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Identity;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _options;

    public RefreshTokenService(ApplicationDbContext db, IJwtTokenService jwt, IOptions<JwtOptions> options)
    {
        _db = db;
        _jwt = jwt;
        _options = options.Value;
    }

    public async Task<RefreshTokenResult?> CreateAsync(string userId, string? deviceInfo, CancellationToken ct = default)
    {
        var refreshToken = _jwt.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo?.Length > 256 ? deviceInfo[..256] : deviceInfo
        });
        await _db.SaveChangesAsync(ct);

        var claims = await GetClaimsForUserAsync(userId, ct);
        var accessToken = _jwt.GenerateAccessToken(claims);
        var accessExpires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        return new RefreshTokenResult(accessToken, refreshToken, accessExpires, expiresAt);
    }

    public async Task<RefreshTokenResult?> RotateAsync(string refreshToken, CancellationToken ct = default)
    {
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, ct);
        if (existing == null)
            return null;

        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;

        var newRefresh = _jwt.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            Token = newRefresh,
            ExpiresAt = expiresAt,
            DeviceInfo = existing.DeviceInfo
        });
        await _db.SaveChangesAsync(ct);

        var claims = await GetClaimsForUserAsync(existing.UserId, ct);
        var accessToken = _jwt.GenerateAccessToken(claims);
        var accessExpires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        return new RefreshTokenResult(accessToken, newRefresh, accessExpires, expiresAt);
    }

    public async Task RevokeAsync(string userId, string? refreshToken, CancellationToken ct = default)
    {
        var query = _db.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked);
        if (!string.IsNullOrEmpty(refreshToken))
            query = query.Where(rt => rt.Token == refreshToken);

        var tokens = await query.ToListAsync(ct);
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<System.Security.Claims.Claim>> GetClaimsForUserAsync(string userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)];

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
            new(System.Security.Claims.ClaimTypes.Email, user.Email ?? ""),
            new("tenant_id", user.TenantId?.ToString() ?? "")
        };
        if (!string.IsNullOrEmpty(user.FullName))
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName));

        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(ct);
        foreach (var role in roles.Where(r => r != null))
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role!));

        return claims;
    }
}
