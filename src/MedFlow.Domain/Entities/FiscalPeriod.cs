using MedFlow.Domain.Common;
using MedFlow.Domain.Enums;

namespace MedFlow.Domain.Entities;

/// <summary>Período fiscal (mes contable).</summary>
public class FiscalPeriod : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }   // 1–12

    public string Name { get; set; } = string.Empty; // ej: "Enero 2026"

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAt { get; set; }
    public string? ClosedByUserId { get; set; }

    public bool IsYearlyClosed { get; set; }
    public DateTime? YearlyClosedAt { get; set; }

    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
}
