using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

/// <summary>Clínica / organización (tenant) en el modelo SaaS.</summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Slug único para subdominio y resolución (ej: clinica-demo).</summary>
    public string Code { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    /// <summary>JSON legacy / compatibilidad; preferir <see cref="TenantSettings"/>.</summary>
    public string? Settings { get; set; }

    public TenantCommercialStatus CommercialStatus { get; set; } = TenantCommercialStatus.Active;

    public Guid? CurrentSubscriptionId { get; set; }
    public TenantSubscription? CurrentSubscription { get; set; }

    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }

    public ICollection<TenantSettings> TenantSettings { get; set; } = [];
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
    public ICollection<TenantSubscriptionHistory> SubscriptionHistories { get; set; } = [];
    public TenantBillingProfile? BillingProfile { get; set; }
}
