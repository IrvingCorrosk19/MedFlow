using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.Models;

public sealed class AccountLoginViewModel
{
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
