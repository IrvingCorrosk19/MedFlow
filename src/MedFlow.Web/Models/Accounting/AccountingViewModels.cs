using MedFlow.Application.Accounting;
using MedFlow.Application.Options;
using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.Models.Accounting;

public sealed class TaxRateFormViewModel
{
    public Guid? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Code { get; set; } = string.Empty;
    [Range(0, 100)] public decimal Rate { get; set; }
    public Guid? TaxAccountId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BankAccountFormViewModel
{
    public Guid? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string BankName { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string Currency { get; set; } = "USD";
    [Range(0, double.MaxValue)] public decimal OpeningBalance { get; set; }
    public Guid? AccountId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AccountFormViewModel
{
    public Guid? Id { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MedFlow.Domain.Enums.AccountType Type { get; set; }
    public Guid? ParentId { get; set; }
    public bool AllowsDirectPosting { get; set; } = true;
}

public sealed class AccountMappingSettingsViewModel
{
    [Required, Display(Name = "Cuenta de Caja/Bancos (prefijo)")]
    public string CashAccountCode { get; set; } = AccountMappingOptions.DefaultCash;

    [Required, Display(Name = "Cuentas por Cobrar (prefijo)")]
    public string ReceivablesAccountCode { get; set; } = AccountMappingOptions.DefaultReceivables;

    [Required, Display(Name = "Ingresos (prefijo)")]
    public string RevenueAccountCode { get; set; } = AccountMappingOptions.DefaultRevenue;

    [Required, Display(Name = "IVA/Impuesto por Pagar (prefijo)")]
    public string TaxPayableAccountCode { get; set; } = AccountMappingOptions.DefaultTaxPayable;

    [Required, Display(Name = "Utilidades Retenidas (prefijo)")]
    public string RetainedEarningsAccountCode { get; set; } = AccountMappingOptions.DefaultRetainedEarnings;
}

public sealed class AccountingDashboardViewModel
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenses;
    public IReadOnlyList<JournalEntryListItemDto> RecentEntries { get; set; } = [];
    public IReadOnlyList<FiscalPeriodDto> OpenPeriods { get; set; } = [];
    public string CurrentPeriodName { get; set; } = "";
}
