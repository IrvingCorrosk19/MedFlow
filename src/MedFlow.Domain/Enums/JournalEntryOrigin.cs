namespace MedFlow.Domain.Enums;

/// <summary>Origen del asiento contable.</summary>
public enum JournalEntryOrigin
{
    /// <summary>Creado manualmente por el usuario.</summary>
    Manual = 0,
    /// <summary>Generado automáticamente al emitir una factura.</summary>
    Invoice = 1,
    /// <summary>Generado automáticamente al registrar un pago.</summary>
    Payment = 2,
    /// <summary>Generado al cancelar una factura.</summary>
    InvoiceCancellation = 3,
    /// <summary>Generado al anular un pago.</summary>
    PaymentCancellation = 4,
    /// <summary>Asiento de cierre de período.</summary>
    PeriodClosing = 5,
    /// <summary>Asiento de apertura.</summary>
    Opening = 6,
}
