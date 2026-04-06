using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

/// <summary>Asiento contable (partida doble).</summary>
public class JournalEntry : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Número secuencial del asiento: AST-2026-000001</summary>
    public string EntryNumber { get; set; } = string.Empty;

    public Guid FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public JournalEntryOrigin Origin { get; set; } = JournalEntryOrigin.Manual;

    /// <summary>ID del documento que originó el asiento (factura, pago, etc.).</summary>
    public Guid? SourceDocumentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime? PostedAt { get; set; }
    public string? PostedByUserId { get; set; }

    /// <summary>Total débitos = Total créditos (siempre en un asiento balanceado).</summary>
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}
