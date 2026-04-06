namespace MedFlow.Domain.Enums;

public enum JournalEntryStatus
{
    /// <summary>Borrador — editable, no afecta el mayor.</summary>
    Draft = 0,
    /// <summary>Publicado — afecta el libro mayor, no editable.</summary>
    Posted = 1,
    /// <summary>Anulado — reversado contablemente.</summary>
    Voided = 2,
}
