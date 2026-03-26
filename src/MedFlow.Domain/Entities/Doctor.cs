using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

public class Doctor : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string? UserId { get; set; } // Links to Identity User

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Speciality { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WorkingHours { get; set; } // JSON or text for schedule
    public string? ConsultationRoom { get; set; }
    public string? Notes { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = [];
    public ICollection<BillingInvoice> BillingInvoices { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}
