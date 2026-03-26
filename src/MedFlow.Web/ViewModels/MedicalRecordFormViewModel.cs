using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class PrescriptionLineViewModel
{
    [StringLength(300)]
    [Display(Name = "Medicamento")]
    public string? MedicationName { get; set; }

    [StringLength(200)]
    [Display(Name = "Dosis")]
    public string? Dosage { get; set; }

    [StringLength(200)]
    [Display(Name = "Frecuencia")]
    public string? Frequency { get; set; }

    [StringLength(200)]
    [Display(Name = "Duración")]
    public string? Duration { get; set; }

    [StringLength(1000)]
    [Display(Name = "Indicaciones")]
    public string? Instructions { get; set; }
}

public class MedicalRecordFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    [Required]
    [Display(Name = "Fecha y hora de consulta")]
    public DateTime VisitDate { get; set; } = DateTime.Now;

    [Display(Name = "Motivo de consulta")]
    [StringLength(500)]
    public string? ChiefComplaint { get; set; }

    [Display(Name = "Diagnóstico")]
    [StringLength(500)]
    public string? Diagnosis { get; set; }

    [Display(Name = "Plan de tratamiento")]
    [StringLength(4000)]
    public string? TreatmentPlan { get; set; }

    [Display(Name = "Notas clínicas")]
    [StringLength(8000)]
    public string? ClinicalNotes { get; set; }

    [Display(Name = "Observaciones")]
    [StringLength(4000)]
    public string? Observations { get; set; }

    [Display(Name = "Talla (cm)")]
    public decimal? HeightCm { get; set; }

    [Display(Name = "Peso (kg)")]
    public decimal? WeightKg { get; set; }

    [Display(Name = "Presión arterial")]
    [StringLength(20)]
    public string? BloodPressure { get; set; }

    [Display(Name = "Frecuencia cardíaca (lpm)")]
    public int? HeartRateBpm { get; set; }

    [Display(Name = "Temperatura (°C)")]
    public decimal? TemperatureCelsius { get; set; }

    public List<PrescriptionLineViewModel> PrescriptionLines { get; set; } = [new()];
}
