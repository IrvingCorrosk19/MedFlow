using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

/// <summary>Línea de débito o crédito de un asiento contable.</summary>
public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Monto débito. Exactamente uno de (Debit, Credit) debe ser > 0.</summary>
    public decimal Debit { get; set; }

    /// <summary>Monto crédito. Exactamente uno de (Debit, Credit) debe ser > 0.</summary>
    public decimal Credit { get; set; }

    /// <summary>Orden de presentación dentro del asiento.</summary>
    public int LineOrder { get; set; }
}
