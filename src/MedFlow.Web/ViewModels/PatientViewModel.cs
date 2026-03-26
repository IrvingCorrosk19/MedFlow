using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class PatientViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El primer nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Primer nombre")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Segundo nombre")]
    public string? SegundoNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Primer apellido")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Segundo apellido")]
    public string? SegundoApellido { get; set; }

    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    [Display(Name = "Sexo")]
    [StringLength(20)]
    public string? Sexo { get; set; }

    [Display(Name = "Tipo de documento")]
    [StringLength(30)]
    public string? TipoDocumento { get; set; }

    [Display(Name = "Número de documento")]
    [StringLength(50)]
    public string? NumeroDocumento { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(30)]
    [Phone(ErrorMessage = "Formato de teléfono no válido")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [StringLength(256)]
    public string? Correo { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(500)]
    public string? Direccion { get; set; }

    [Display(Name = "Contacto de emergencia (nombre)")]
    [StringLength(200)]
    public string? ContactoEmergenciaNombre { get; set; }

    [Display(Name = "Contacto de emergencia (teléfono)")]
    [StringLength(30)]
    [Phone(ErrorMessage = "Teléfono no válido")]
    public string? ContactoEmergenciaTelefono { get; set; }

    [Display(Name = "Alergias")]
    [StringLength(1000)]
    public string? Alergias { get; set; }

    [Display(Name = "Observaciones")]
    [StringLength(4000)]
    public string? Observaciones { get; set; }

    [Display(Name = "Estado activo")]
    public bool EstadoActivo { get; set; } = true;
}
