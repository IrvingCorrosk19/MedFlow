namespace MedFlow.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleId { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
