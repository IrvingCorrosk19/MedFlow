using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class RoleAdminService : IRoleAdminService
{
    private const string ProtectedRoleSuperAdmin = "SuperAdmin";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public RoleAdminService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext db, IAuditLogService audit)
    {
        _roleManager = roleManager;
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var list = new List<RoleListItem>();
        foreach (var r in roles)
        {
            var permCount = await _db.RolePermissions.CountAsync(x => x.RoleId == r.Id, cancellationToken);
            var userCount = await _db.UserRoles.CountAsync(x => x.RoleId == r.Id, cancellationToken);
            list.Add(new RoleListItem
            {
                Id = r.Id,
                Name = r.Name ?? "",
                Description = r.Description,
                IsActive = r.IsActive,
                PermissionCount = permCount,
                UserCount = userCount
            });
        }
        return list;
    }

    public async Task<RoleDetails?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var r = await _roleManager.FindByIdAsync(id);
        if (r == null) return null;
        var permIds = await _db.RolePermissions.Where(x => x.RoleId == id).Select(x => x.PermissionId).ToListAsync(cancellationToken);
        var userCount = await _db.UserRoles.CountAsync(x => x.RoleId == id, cancellationToken);
        var name = r.Name ?? "";
        var canDelete = userCount == 0
            && !string.Equals(name, ProtectedRoleSuperAdmin, StringComparison.OrdinalIgnoreCase);
        return new RoleDetails
        {
            Id = r.Id,
            Name = name,
            Description = r.Description,
            IsActive = r.IsActive,
            UserCount = userCount,
            CanDelete = canDelete,
            PermissionIds = permIds
        };
    }

    public async Task<(bool Ok, string? Error)> CreateAsync(RoleAdminCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "El nombre del rol es obligatorio.");
        if (await _roleManager.RoleExistsAsync(dto.Name.Trim()))
            return (false, "Ya existe un rol con ese nombre.");

        var role = new ApplicationRole
        {
            Name = dto.Name.Trim(),
            NormalizedName = dto.Name.Trim().ToUpperInvariant(),
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        await _audit.LogAsync(new AuditLogWriteDto("Create", "Security", "Role", role.Id, $"Rol creado: {role.Name}"), cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(RoleAdminUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(dto.Id);
        if (role == null)
            return (false, "Rol no encontrado.");

        var oldJson = JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive });

        if (!string.Equals(role.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (await _roleManager.RoleExistsAsync(dto.Name.Trim()))
                return (false, "Ya existe otro rol con ese nombre.");
        }

        role.Name = dto.Name.Trim();
        role.NormalizedName = dto.Name.Trim().ToUpperInvariant();
        role.Description = dto.Description?.Trim();
        role.IsActive = dto.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        var newJson = JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive });
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Security", "Role", role.Id, "Rol actualizado", oldJson, newJson),
            null, null, null, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return (false, "Rol no encontrado.");

        if (string.Equals(role.Name, ProtectedRoleSuperAdmin, StringComparison.OrdinalIgnoreCase))
            return (false, "Este rol del sistema no puede eliminarse.");

        var userCount = await _db.UserRoles.CountAsync(x => x.RoleId == roleId, cancellationToken);
        if (userCount > 0)
            return (false, $"No se puede eliminar el rol: hay {userCount} usuario(s) asignado(s).");

        var rps = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        if (rps.Count > 0)
        {
            _db.RolePermissions.RemoveRange(rps);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var del = await _roleManager.DeleteAsync(role);
        if (!del.Succeeded)
            return (false, string.Join(" ", del.Errors.Select(e => e.Description)));

        await _audit.LogAsync(new AuditLogWriteDto("Delete", "Security", "Role", roleId, $"Rol eliminado: {role.Name}"), cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetPermissionsAsync(string roleId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return (false, "Rol no encontrado.");

        var existing = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        _db.RolePermissions.RemoveRange(existing);

        var distinctIds = permissionIds.Distinct().ToList();
        var validIds = await _db.Permissions
            .Where(p => distinctIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var pid in validIds)
        {
            await _db.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        role.UpdatedAt = DateTime.UtcNow;
        await _roleManager.UpdateAsync(role);

        await _audit.LogAsync(new AuditLogWriteDto("AssignPermissions", "Security", "Role", roleId, $"Permisos asignados al rol {role.Name}"), cancellationToken);
        return (true, null);
    }
}
