using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Identity;
using MedFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class PatientPortalEnableService : IPatientPortalEnableService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogService _audit;

    public PatientPortalEnableService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ITenantContext tenant,
        IAuditLogService audit)
    {
        _db = db;
        _userManager = userManager;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<(bool Success, string? UserId, string? TemporaryPassword, string? Error)> EnablePortalForPatientAsync(Guid patientId, string? preferredPassword, CancellationToken cancellationToken = default)
    {
        if (!_tenant.TenantId.HasValue)
            return (false, null, null, "No hay tenant en contexto.");

        var patient = await _db.Patients.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == _tenant.TenantId && !p.IsDeleted, cancellationToken);
        if (patient == null)
            return (false, null, null, "Paciente no encontrado.");

        if (!string.IsNullOrWhiteSpace(patient.UserId))
            return (false, null, null, "El paciente ya tiene acceso al portal.");

        if (string.IsNullOrWhiteSpace(patient.Correo))
            return (false, null, null, "El paciente debe tener un correo electrónico para acceder al portal.");

        var existingUser = await _userManager.FindByEmailAsync(patient.Correo);
        if (existingUser != null)
        {
            if (existingUser.TenantId != _tenant.TenantId)
                return (false, null, null, "Ya existe un usuario con ese correo en otra organización.");
            if (await _userManager.IsInRoleAsync(existingUser, "Patient"))
            {
                var otherPatient = await _db.Patients.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.UserId == existingUser.Id && p.Id != patientId && !p.IsDeleted, cancellationToken);
                if (otherPatient != null)
                    return (false, null, null, "El correo ya está en uso por otro paciente con acceso al portal.");
                patient.UserId = existingUser.Id;
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(new AuditLogWriteDto("EnablePortal", "PatientPortal", nameof(Patient), patient.Id.ToString(),
                    $"Portal habilitado vinculando usuario existente {existingUser.Email}"), cancellationToken);
                return (true, existingUser.Id, null, null);
            }
        }

        var tempPassword = preferredPassword ?? GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            UserName = patient.Correo,
            Email = patient.Correo,
            EmailConfirmed = true,
            TenantId = _tenant.TenantId,
            FirstName = patient.PrimerNombre,
            LastName = patient.PrimerApellido,
            FullName = patient.NombreCompleto,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return (false, null, null, string.Join("; ", result.Errors.Select(e => e.Description)));

        var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return (false, null, null, "No se pudo asignar el rol Patient.");
        }

        patient.UserId = user.Id;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("EnablePortal", "PatientPortal", nameof(Patient), patient.Id.ToString(),
            $"Portal habilitado para paciente. Usuario creado: {user.Email}"), cancellationToken);

        return (true, user.Id, tempPassword, null);
    }

    public async Task<bool> DisablePortalForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);
        if (patient == null || string.IsNullOrEmpty(patient.UserId))
            return false;

        var userId = patient.UserId;
        patient.UserId = null;
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.RemoveFromRoleAsync(user, "Patient");
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
        }

        await _audit.LogAsync(new AuditLogWriteDto("DisablePortal", "PatientPortal", nameof(Patient), patient.Id.ToString(),
            "Portal deshabilitado para paciente."), cancellationToken);
        return true;
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789!";
        var r = new Random();
        var pwd = new char[10];
        pwd[0] = chars[r.Next(52)];
        pwd[1] = chars[r.Next(52, chars.Length)];
        for (var i = 2; i < pwd.Length; i++)
            pwd[i] = chars[r.Next(chars.Length)];
        return new string(pwd);
    }
}
