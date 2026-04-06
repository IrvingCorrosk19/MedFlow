using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

/// <summary>Cuenta bancaria registrada en el sistema.</summary>
public class BankAccount : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;          // "Banco Nacional - Cta Corriente"
    public string BankName { get; set; } = string.Empty;       // "Banco Nacional"
    public string? AccountNumber { get; set; }
    public string? Currency { get; set; } = "USD";

    /// <summary>Saldo inicial al momento de crear la cuenta en el sistema.</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Cuenta contable vinculada (cuenta Bancos del plan de cuentas).</summary>
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }

    public ICollection<BankTransaction> Transactions { get; set; } = [];
}
