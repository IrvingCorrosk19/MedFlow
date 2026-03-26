using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.Models.Onboarding;

public sealed class OnboardingStep1Vm
{
    [Required(ErrorMessage = "El nombre de la clínica es obligatorio.")]
    [StringLength(200)]
    [Display(Name = "Nombre de la clínica")]
    public string ClinicName { get; set; } = "";

    [Required(ErrorMessage = "El código único es obligatorio.")]
    [StringLength(64, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Solo minúsculas, números y guiones (sin espacios).")]
    [Display(Name = "Código único (subdominio)")]
    public string Code { get; set; } = "";

    [EmailAddress(ErrorMessage = "Correo inválido.")]
    [StringLength(256)]
    [Display(Name = "Correo de la clínica")]
    public string? ClinicEmail { get; set; }

    [StringLength(40)]
    [Display(Name = "Teléfono")]
    public string? Phone { get; set; }

    [StringLength(500)]
    [Display(Name = "Dirección")]
    public string? Address { get; set; }
}

public sealed class OnboardingStep2Vm
{
    [Required(ErrorMessage = "Seleccione un plan.")]
    public Guid? SubscriptionPlanId { get; set; }

    public bool StartWithTrial { get; set; } = true;
}

public sealed class OnboardingStep3Vm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string AdminFirstName { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(120)]
    public string AdminLastName { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class OnboardingStep4Vm
{
    [Required(ErrorMessage = "Seleccione zona horaria.")]
    [StringLength(120)]
    [Display(Name = "Zona horaria")]
    public string TimeZoneId { get; set; } = "America/Mexico_City";

    [Required(ErrorMessage = "Seleccione formato de fecha.")]
    [StringLength(32)]
    [Display(Name = "Formato de fecha")]
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    [Required(ErrorMessage = "Seleccione moneda.")]
    [StringLength(8)]
    [Display(Name = "Moneda")]
    public string Currency { get; set; } = "USD";

    [Required(ErrorMessage = "Seleccione idioma.")]
    [StringLength(12)]
    [Display(Name = "Idioma")]
    public string Language { get; set; } = "es";
}

public sealed class OnboardingSuccessVm
{
    public string TenantCode { get; set; } = "";
    public string? AdminEmail { get; set; }
    public string? HostHint { get; set; }
}
