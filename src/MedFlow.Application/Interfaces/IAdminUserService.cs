namespace MedFlow.Application.Interfaces;

public class AdminUserListItem
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public class AdminUserDetails
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public class AdminUserCreateDto
{
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public IReadOnlyList<string> RoleNames { get; set; } = [];
}

public class AdminUserUpdateDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? NewPassword { get; set; }
    public IReadOnlyList<string> RoleNames { get; set; } = [];
}

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdminUserDetails?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CreateAsync(AdminUserCreateDto dto, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> UpdateAsync(AdminUserUpdateDto dto, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SetActiveAsync(string id, bool active, CancellationToken cancellationToken = default);
}
