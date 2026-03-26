using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public class RoleListItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
}

public class RoleDetails
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public bool CanDelete { get; set; }
    public IReadOnlyList<Guid> PermissionIds { get; set; } = [];
}

public class RoleAdminCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RoleAdminUpdateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDetails?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CreateAsync(RoleAdminCreateDto dto, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> UpdateAsync(RoleAdminUpdateDto dto, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> DeleteAsync(string roleId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SetPermissionsAsync(string roleId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default);
}

public interface IPermissionCatalogService
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
}
