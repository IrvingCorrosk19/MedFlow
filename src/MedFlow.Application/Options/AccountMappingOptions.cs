namespace MedFlow.Application.Options;

public class AccountMappingOptions
{
    public const string SectionName = "AccountMapping";

    // Default code prefixes exposed as constants for use in ViewModels and tests.
    public const string DefaultCash             = "1100";
    public const string DefaultReceivables      = "1200";
    public const string DefaultRevenue          = "4100";
    public const string DefaultTaxPayable       = "2300";
    public const string DefaultRetainedEarnings = "3300";

    /// <summary>Cash/bank account code prefix (default: 1100)</summary>
    public string CashAccountCode { get; set; } = DefaultCash;

    /// <summary>Accounts receivable code prefix (default: 1200)</summary>
    public string ReceivablesAccountCode { get; set; } = DefaultReceivables;

    /// <summary>Revenue account code prefix (default: 4100)</summary>
    public string RevenueAccountCode { get; set; } = DefaultRevenue;

    /// <summary>Tax payable account code prefix (default: 2300)</summary>
    public string TaxPayableAccountCode { get; set; } = DefaultTaxPayable;

    /// <summary>Retained earnings account code prefix (default: 3300)</summary>
    public string RetainedEarningsAccountCode { get; set; } = DefaultRetainedEarnings;
}
