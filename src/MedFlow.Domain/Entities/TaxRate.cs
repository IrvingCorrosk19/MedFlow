using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

/// <summary>Tasa de impuesto configurable por tenant (IVA, IGV, ITBMS, etc.).</summary>
public class TaxRate : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Nombre del impuesto, ej: "IVA 19%", "IGV 18%", "ITBMS 7%"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Código corto, ej: "IVA", "IGV"</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Porcentaje, ej: 19.00 para 19%</summary>
    public decimal Rate { get; set; }

    /// <summary>Cuenta contable para el impuesto por cobrar/pagar.</summary>
    public Guid? TaxAccountId { get; set; }
    public Account? TaxAccount { get; set; }

    /// <summary>Si true, es el impuesto predeterminado para nuevas facturas.</summary>
    public bool IsDefault { get; set; }
}
