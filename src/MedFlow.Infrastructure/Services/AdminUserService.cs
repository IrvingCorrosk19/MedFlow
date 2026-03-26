using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionLimitService _limits;
    private readonly IAuditLogService _audit;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ITenantContext tenant,
        ISubscriptionLimitService limits,
        IAuditLogService audit)
    {
        _userManager = userManager;
        _db = db;
        _tenant = tenant;
        _limits = limits;
        _audit = audit;
    }

    public async Task<IReadOnlyList<AdminUserListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var list = new List<AdminUserListItem>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new AdminUserListItem
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                LastLoginAt = u.LastLoginAt,
                Roles = roles.ToList()
            });
        }
        return list;
    }

    public async Task<AdminUserDetails?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return null;
        var roles = await _userManager.GetRolesAsync(u);
        return new AdminUserDetails
        {
            Id = u.Id,
            Email = u.Email,
            UserName = u.UserName,
            FirstName = u.FirstName,
            MiddleName = u.MiddleName,
            LastName = u.LastName,
            SecondLastName = u.SecondLastName,
            PhoneNumber = u.PhoneNumber,
            FullName = u.FullName,
            IsActive = u.IsActive,
            IsLocked = u.IsLocked,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            Roles = roles.ToList()
        };
    }

    public async Task<(bool Ok, string? Error)> CreateAsync(AdminUserCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, "El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return (false, "Nombre y primer apellido son obligatorios.");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return (false, "La contraseña debe tener al menos 6 caracteres.");

        var existing = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existing != null)
            return (false, "Ya existe un usuario con ese correo.");

        if (_tenant.TenantId.HasValue)
        {
            var chk = await _limits.CanCreateUserAsync(_tenant.TenantId.Value, cancellationToken);
            if (!chk.Allowed)
            {
                var msg = chk.Suggestion != null ? $"{chk.Message} {chk.Suggestion}" : chk.Message;
                return (false, msg);
            }
        }

        var userName = string.IsNullOrWhiteSpace(dto.UserName) ? dto.Email.Trim() : dto.UserName!.Trim();
        if (await _userManager.FindByNameAsync(userName) != null)
            return (false, "El nombre de usuario ya está en uso.");

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = dto.Email.Trim(),
            EmailConfirmed = true,
            FirstName = dto.FirstName?.Trim(),
            MiddleName = dto.MiddleName?.Trim(),
            LastName = dto.LastName?.Trim(),
            SecondLastName = dto.SecondLastName?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = dto.IsActive,
            TenantId = _tenant.TenantId,
            CreatedAt = DateTime.UtcNow
        };
        user.FullName = string.IsNullOrWhiteSpace(user.ComputeFullName()) ? userName : user.ComputeFullName();

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        if (dto.RoleNames.Count > 0)
        {
            var rr = await _userManager.AddToRolesAsync(user, dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()));
            if (!rr.Succeeded)
                return (false, string.Join(" ", rr.Errors.Select(e => e.Description)));
        }

        await _audit.LogAsync(new AuditLogWriteDto("Create", "Security", "User", user.Id, $"Usuario creado: {user.Email}"), cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(AdminUserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return (false, "Nombre y primer apellido son obligatorios.");

        var rolesBefore = await _userManager.GetRolesAsync(user);
        var oldJson = JsonSerializer.Serialize(new
        {
            user.Email,
            user.UserName,
            user.IsActive,
            user.IsLocked,
            Roles = rolesBefore.Order().ToArray()
        });

        if (!string.Equals(user.Email, dto.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var other = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (other != null && other.Id != user.Id)
                return (false, "El correo ya está en uso.");
        }

        user.Email = dto.Email.Trim();
        user.UserName = string.IsNullOrWhiteSpace(dto.UserName) ? dto.Email.Trim() : dto.UserName!.Trim();
        user.FirstName = dto.FirstName?.Trim();
        user.MiddleName = dto.MiddleName?.Trim();
        user.LastName = dto.LastName?.Trim();
        user.SecondLastName = dto.SecondLastName?.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.IsActive = dto.IsActive;
        user.IsLocked = dto.IsLocked;
        user.FullName = user.ComputeFullName();
        if (string.IsNullOrWhiteSpace(user.FullName))
            user.FullName = user.UserName;
        user.UpdatedAt = DateTime.UtcNow;

        var ur = await _userManager.UpdateAsync(user);
        if (!ur.Succeeded)
            return (false, string.Join(" ", ur.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pr = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!pr.Succeeded)
                return (false, string.Join(" ", pr.Errors.Select(e => e.Description)));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var target = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toRemove = currentRoles.Where(r => !target.Contains(r)).ToList();
        var toAdd = target.Where(r => !currentRoles.Contains(r)).ToList();
        if (toRemove.Count > 0)
        {
            var x = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!x.Succeeded) return (false, string.Join(" ", x.Errors.Select(e => e.Description)));
        }
        if (toAdd.Count > 0)
        {
            var x = await _userManager.AddToRolesAsync(user, toAdd);
            if (!x.Succeeded) return (false, string.Join(" ", x.Errors.Select(e => e.Description)));
        }

        var rolesAfter = await _userManager.GetRolesAsync(user);
        var newJson = JsonSerializer.Serialize(new
        {
            user.Email,
            user.UserName,
            user.IsActive,
            user.IsLocked,
            Roles = rolesAfter.Order().ToArray()
        });
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Security", "User", user.Id, "Usuario actualizado", oldJson, newJson), cancellationToken);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetActiveAsync(string id, bool active, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");
        user.IsActive = active;
        user.UpdatedAt = DateTime.UtcNow;
        var r = await _userManager.UpdateAsync(user);
        if (!r.Succeeded)
            return (false, string.Join(" ", r.Errors.Select(e => e.Description)));
        await _audit.LogAsync(new AuditLogWriteDto(active ? "Activate" : "Deactivate", "Security", "User", id, active ? "Usuario activado" : "Usuario desactivado"), cancellationToken);
        return (true, null);
    }
}
