using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.Areas.PatientPortal.Models;

public sealed class PatientProfileUpdateViewModel
{
    [Phone]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string? Correo { get; set; }

    [Display(Name = "Dirección")]
    [MaxLength(500)]
    public string? Direccion { get; set; }

    [Display(Name = "Contacto de emergencia - Nombre")]
    [MaxLength(200)]
    public string? ContactoEmergenciaNombre { get; set; }

    [Phone]
    [Display(Name = "Contacto de emergencia - Teléfono")]
    public string? ContactoEmergenciaTelefono { get; set; }
}
