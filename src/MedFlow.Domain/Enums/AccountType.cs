namespace MedFlow.Domain.Enums;

/// <summary>Tipo de cuenta contable según la estructura del plan de cuentas.</summary>
public enum AccountType
{
    /// <summary>Activos — recursos que posee la empresa.</summary>
    Asset = 1,
    /// <summary>Pasivos — obligaciones con terceros.</summary>
    Liability = 2,
    /// <summary>Patrimonio / Capital.</summary>
    Equity = 3,
    /// <summary>Ingresos operacionales y no operacionales.</summary>
    Revenue = 4,
    /// <summary>Gastos operacionales y no operacionales.</summary>
    Expense = 5,
    /// <summary>Costos de servicios / ventas.</summary>
    Cost = 6,
}
