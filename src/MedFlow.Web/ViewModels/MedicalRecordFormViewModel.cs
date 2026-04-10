using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class PrescriptionFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid MedicalRecordId { get; set; }

    public Guid? PatientId { get; set; }

    [Required(ErrorMessage = "El nombre del medicamento es obligatorio.")]
    [StringLength(300)]
    [Display(Name = "Medicamento")]
    public string MedicationName { get; set; } = string.Empty;

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
    [Display(Name = "Indicaciones especiales")]
    public string? Instructions { get; set; }

    [StringLength(200)]
    [Display(Name = "Nombre del médico prescriptor")]
    public string? PrescriberName { get; set; }

    [StringLength(100)]
    [Display(Name = "Cédula / Matrícula")]
    public string? PrescriberLicense { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow.Date;
}

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
    [Range(50, 250, ErrorMessage = "La talla debe estar entre 50 y 250 cm.")]
    public decimal? HeightCm { get; set; }

    [Display(Name = "Peso (kg)")]
    [Range(1, 500, ErrorMessage = "El peso debe estar entre 1 y 500 kg.")]
    public decimal? WeightKg { get; set; }

    [Display(Name = "Presión arterial")]
    [StringLength(20)]
    [RegularExpression(@"^\d{2,3}/\d{2,3}$", ErrorMessage = "Formato esperado: 120/80")]
    public string? BloodPressure { get; set; }

    [Display(Name = "Frecuencia cardíaca (lpm)")]
    [Range(30, 300, ErrorMessage = "La frecuencia cardíaca debe estar entre 30 y 300 lpm.")]
    public int? HeartRateBpm { get; set; }

    [Display(Name = "Temperatura (°C)")]
    [Range(30.0, 45.0, ErrorMessage = "La temperatura debe estar entre 30.0 y 45.0 °C.")]
    public decimal? TemperatureCelsius { get; set; }

    public List<PrescriptionLineViewModel> PrescriptionLines { get; set; } = [new()];
}
