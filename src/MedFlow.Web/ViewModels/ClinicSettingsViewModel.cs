using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class ClinicSettingsViewModel
{
    [Required(ErrorMessage = "El nombre de la clínica es obligatorio.")]
    [MaxLength(200)]
    [Display(Name = "Nombre de la clínica")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Razón social / Nombre legal")]
    public string? LegalName { get; set; }

    [MaxLength(50)]
    [Display(Name = "RUC / NIT / RFC")]
    public string? TaxId { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [MaxLength(200)]
    [Display(Name = "Correo de contacto")]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(50)]
    [Display(Name = "Teléfono")]
    public string? Phone { get; set; }

    [MaxLength(500)]
    [Display(Name = "Dirección")]
    public string? Address { get; set; }

    [MaxLength(500)]
    [Display(Name = "URL del logo")]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    [Display(Name = "Color primario")]
    public string? PrimaryColor { get; set; }

    [MaxLength(20)]
    [Display(Name = "Color secundario")]
    public string? SecondaryColor { get; set; }

    [MaxLength(100)]
    [Display(Name = "Zona horaria")]
    public string? Timezone { get; set; }

    [MaxLength(10)]
    [Display(Name = "Moneda")]
    public string? Currency { get; set; }

    [MaxLength(5)]
    [Display(Name = "Hora apertura")]
    public string? BusinessHoursStart { get; set; }

    [MaxLength(5)]
    [Display(Name = "Hora cierre")]
    public string? BusinessHoursEnd { get; set; }
}
