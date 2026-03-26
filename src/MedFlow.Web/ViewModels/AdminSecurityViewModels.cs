using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class AdminUserFormViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }

    public List<string> RoleNames { get; set; } = new();

    public IReadOnlyList<string> AllRoleNames { get; set; } = Array.Empty<string>();
}

public class RoleFormViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RolePermissionsViewModel
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionModuleGroupVm> Modules { get; set; } = new();
}

public class PermissionModuleGroupVm
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionCheckboxVm> Items { get; set; } = new();
}

public class PermissionCheckboxVm
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
