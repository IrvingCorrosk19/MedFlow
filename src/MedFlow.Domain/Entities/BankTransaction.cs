using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

/// <summary>Transacción bancaria (línea de estado de cuenta).</summary>
public class BankTransaction : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public BankTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }

    /// <summary>Si la transacción ya fue conciliada con un asiento contable.</summary>
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }

    /// <summary>Asiento contable al que fue conciliada.</summary>
    public Guid? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    /// <summary>Pago clínico vinculado (si aplica).</summary>
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
