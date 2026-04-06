using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

/// <summary>Cuenta del plan contable (catálogo de cuentas).</summary>
public class Account : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Código de cuenta, ej: 1100, 4100.01</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public AccountType Type { get; set; }

    /// <summary>Cuenta padre para jerarquía (null = cuenta raíz).</summary>
    public Guid? ParentId { get; set; }
    public Account? Parent { get; set; }

    /// <summary>Cuentas hijo en la jerarquía.</summary>
    public ICollection<Account> Children { get; set; } = [];

    /// <summary>True = cuenta de movimiento (permite asientos). False = cuenta de grupo/resumen.</summary>
    public bool AllowsDirectPosting { get; set; } = true;

    /// <summary>Saldo actual calculado (denormalizado para performance).</summary>
    public decimal CurrentBalance { get; set; } = 0;

    public ICollection<JournalEntryLine> JournalLines { get; set; } = [];
}
