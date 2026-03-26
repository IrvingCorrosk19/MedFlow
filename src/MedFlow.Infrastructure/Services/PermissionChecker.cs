using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class PermissionChecker : IPermissionChecker
{
    private const string SuperAdminRole = "SuperAdmin";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenant;

    public PermissionChecker(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ITenantContext tenant)
    {
        _db = db;
        _userManager = userManager;
        _tenant = tenant;
    }

    public async Task<bool> UserIsInSuperAdminRoleAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Los usuarios plataforma tienen TenantId=null; el filtro global puede ocultarlos si hay tenant resuelto.
        var prev = _tenant.IgnoreTenantFilter;
        _tenant.SetIgnoreTenantFilter(true);
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, SuperAdminRole);
        }
        finally
        {
            _tenant.SetIgnoreTenantFilter(prev);
        }
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (await UserIsInSuperAdminRoleAsync(userId, cancellationToken))
        {
            return await _db.Permissions
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);
        }

        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return Array.Empty<string>();

        return await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_db.Permissions.AsNoTracking().Where(p => !p.IsDeleted),
                rp => rp.PermissionId,
                p => p.Id,
                (_, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserHasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (await UserIsInSuperAdminRoleAsync(userId, cancellationToken))
            return true;

        var codes = await GetPermissionCodesForUserAsync(userId, cancellationToken);
        return codes.Contains(permissionCode);
    }
}
