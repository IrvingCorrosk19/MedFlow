using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Identity;

public sealed class TenantStaffAuthService : ITenantStaffAuthService
{
    private static readonly HashSet<string> StaffRoles =
    [
        "Admin", "Reception", "Doctor", "Billing", "Staff"
    ];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ApplicationDbContext _db;

    public TenantStaffAuthService(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenService refreshTokens,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _refreshTokens = refreshTokens;
        _db = db;
    }

    public async Task<StaffJwtLoginResult?> LoginAsync(StaffJwtLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.TenantCode))
            return null;

        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == request.TenantCode.Trim() && !t.IsDeleted, cancellationToken);
        if (tenant == null)
            return null;

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null || user.TenantId != tenant.Id)
            return null;

        if (!user.IsActive || user.IsLocked)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
            return null;

        // Patient debe usar /api/v1/mobile/auth/login (portal paciente).
        if (roles.Any(r => string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)))
            return null;

        if (!roles.Any(r => StaffRoles.Contains(r)))
            return null;

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return null;

        var tokenResult = await _refreshTokens.CreateAsync(user.Id, deviceInfo: "staff-api", cancellationToken);
        if (tokenResult == null)
            return null;

        var roleList = roles.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();

        return new StaffJwtLoginResult(
            tokenResult.AccessToken,
            tokenResult.RefreshToken,
            tokenResult.AccessTokenExpiresAt,
            tokenResult.RefreshTokenExpiresAt,
            new StaffJwtUserInfo(
                user.Id,
                user.Email ?? "",
                user.FullName,
                tenant.Id,
                tenant.Name,
                roleList));
    }
}
