using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class DoctorViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Nombre es requerido")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Apellido es requerido")]
    [StringLength(100)]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Especialidad")]
    [StringLength(100)]
    public string? Speciality { get; set; }

    [Display(Name = "Cédula/Licencia")]
    [StringLength(50)]
    public string? LicenseNumber { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(30)]
    [Phone]
    public string? Phone { get; set; }

    [Display(Name = "Correo")]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "Horario")]
    public string? WorkingHours { get; set; }

    [Display(Name = "Consultorio")]
    [StringLength(50)]
    public string? ConsultationRoom { get; set; }

    [Display(Name = "Notas")]
    public string? Notes { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}
