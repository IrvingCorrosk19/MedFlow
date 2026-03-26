using MedFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MedFlow.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    /// <summary>Null = rol plantilla global; opcional para roles por tenant.</summary>
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
