using System.Linq;
using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

public class Patient : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }

    public DateTime? FechaNacimiento { get; set; }
    public string? Sexo { get; set; }
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }

    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }

    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }

    public string? Alergias { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>Identity User ID para acceso al portal del paciente. Null = sin acceso.</summary>
    public string? UserId { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = [];
    public ICollection<BillingInvoice> BillingInvoices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];

    public string NombreCompleto =>
        string.Join(" ", new[] { PrimerNombre, SegundoNombre, PrimerApellido, SegundoApellido }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

    public string FullName => NombreCompleto;
}
