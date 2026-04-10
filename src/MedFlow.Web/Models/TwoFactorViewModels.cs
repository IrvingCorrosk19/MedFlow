using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.Models;

public class LoginWith2faViewModel
{
    [Required(ErrorMessage = "Ingrese el código de verificación.")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")]
    [DataType(DataType.Text)]
    [Display(Name = "Código de autenticador")]
    public string? TwoFactorCode { get; set; }

    [Display(Name = "Recordar este dispositivo")]
    public bool RememberMachine { get; set; }
}

public class EnableAuthenticatorViewModel
{
    [Required(ErrorMessage = "Ingrese el código de verificación.")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")]
    [DataType(DataType.Text)]
    [Display(Name = "Código de verificación")]
    public string? Code { get; set; }

    public string? SharedKey { get; set; }
    public string? AuthenticatorUri { get; set; }
}
