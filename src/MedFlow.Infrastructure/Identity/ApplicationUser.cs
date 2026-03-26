using MedFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MedFlow.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }

    /// <summary>Denormalizado para búsquedas; se mantiene al guardar.</summary>
    public string? FullName { get; set; }

    /// <summary>Null = usuario plataforma (p. ej. SuperAdmin).</summary>
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string ComputeFullName() =>
        string.Join(" ", new[] { FirstName, MiddleName, LastName, SecondLastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
}
