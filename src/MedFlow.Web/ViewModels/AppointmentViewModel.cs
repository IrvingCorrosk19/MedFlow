using System.ComponentModel.DataAnnotations;
using MedFlow.Domain.Enums;

namespace MedFlow.Web.ViewModels;

public class AppointmentViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Paciente es requerido")]
    [Display(Name = "Paciente")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Doctor es requerido")]
    [Display(Name = "Doctor")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Fecha es requerida")]
    [Display(Name = "Fecha")]
    [DataType(DataType.Date)]
    public DateTime ScheduledDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Hora inicio es requerida")]
    [Display(Name = "Hora Inicio")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "Hora fin es requerida")]
    [Display(Name = "Hora Fin")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Motivo")]
    [StringLength(500)]
    public string? Reason { get; set; }

    [Display(Name = "Notas")]
    public string? Notes { get; set; }

    [Display(Name = "Consultorio")]
    [StringLength(50)]
    public string? ConsultationRoom { get; set; }

    [Display(Name = "Estado")]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}
