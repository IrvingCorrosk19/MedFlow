using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedFlow.Infrastructure.Identity;

public sealed class MobileAuthService : IMobileAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenService _refreshToken;
    private readonly IPatientPortalService _portal;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _configuration;

    public MobileAuthService(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenService refreshToken,
        IPatientPortalService portal,
        ApplicationDbContext db,
        ITenantContext tenant,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _refreshToken = refreshToken;
        _portal = portal;
        _db = db;
        _tenant = tenant;
        _configuration = configuration;
    }

    public async Task<MobileLoginResult?> LoginAsync(MobileLoginRequest request, CancellationToken ct = default)
    {
        var tenantId = request.TenantId ?? _tenant.TenantId
            ?? await ResolveTenantFromRequestAsync(request.TenantCode, ct);
        if (!tenantId.HasValue)
            return null;

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return null;

        if (!await _userManager.IsInRoleAsync(user, "Patient"))
            return null;

        if (user.TenantId != tenantId)
            return null;

        if (!user.IsActive || user.IsLocked)
            return null;

        var ok = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
            return null;

        var patientId = await _portal.GetPatientIdByUserIdAsync(user.Id, ct);
        if (!patientId.HasValue)
            return null;

        var options = await _portal.GetOptionsAsync(tenantId.Value, ct);
        if (!options.Enabled)
            return null;

        var result = await _refreshToken.CreateAsync(user.Id, request.DeviceInfo, ct);
        if (result == null)
            return null;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        return new MobileLoginResult(
            result.AccessToken,
            result.RefreshToken,
            result.AccessTokenExpiresAt,
            result.RefreshTokenExpiresAt,
            new MobileUserInfo(user.Id, user.Email ?? "", user.FullName, tenantId.Value, tenant?.Name ?? ""),
            patientId.Value);
    }

    public async Task<MobileLoginResult?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        // Resolvemos el userId desde la base de datos ANTES de rotar,
        // evitando parsear el JWT sin validación de firma.
        var existingToken = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, ct);
        if (existingToken == null)
            return null;

        var user = await _userManager.FindByIdAsync(existingToken.UserId);
        if (user == null || !user.IsActive || user.IsLocked)
            return null;

        var patientId = await _portal.GetPatientIdByUserIdAsync(user.Id, ct);
        if (!patientId.HasValue)
            return null;

        // Ahora rotamos el token sabiendo que el usuario es válido.
        var result = await _refreshToken.RotateAsync(refreshToken, ct);
        if (result == null)
            return null;

        var tenantId = user.TenantId ?? Guid.Empty;
        var tenant = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        return new MobileLoginResult(
            result.AccessToken,
            result.RefreshToken,
            result.AccessTokenExpiresAt,
            result.RefreshTokenExpiresAt,
            new MobileUserInfo(user.Id, user.Email ?? "", user.FullName, tenantId, tenant?.Name ?? ""),
            patientId.Value);
    }

    public Task LogoutAsync(string userId, string? refreshToken, CancellationToken ct = default) =>
        _refreshToken.RevokeAsync(userId, refreshToken, ct);

    private async Task<Guid?> ResolveTenantFromRequestAsync(string? tenantCode, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == tenantCode.Trim() && !t.IsDeleted, ct);
            return tenant?.Id;
        }

        // Leer desde config, igual que TenantResolutionMiddleware — sin hardcodear "demo".
        var defaultCode = _configuration["TenantResolution:DefaultTenantCode"] ?? "demo";
        var def = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == defaultCode && !t.IsDeleted, ct);
        return def?.Id;
    }
}
