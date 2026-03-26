using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

public class MedicalRecord : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public DateTime VisitDate { get; set; }

    public string? ChiefComplaint { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? Observations { get; set; }

    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRateBpm { get; set; }
    public decimal? TemperatureCelsius { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = [];
    public ICollection<MedicalAttachment> Attachments { get; set; } = [];
}
