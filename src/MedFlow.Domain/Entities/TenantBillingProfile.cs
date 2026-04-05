using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

public class TenantBillingProfile : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string? BillingEmail { get; set; }
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Country { get; set; }
    public string? StateProvince { get; set; }
    public string? City { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }

    public BillingProvider BillingProvider { get; set; } = BillingProvider.None;
    public string? ExternalCustomerId { get; set; }
    public string? PreferredCurrency { get; set; } = "USD";
}
