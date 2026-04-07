namespace MedFlow.Application.Options;

public class AccountMappingOptions
{
    public const string SectionName = "AccountMapping";

    /// <summary>Cash/bank account code prefix (default: 1100)</summary>
    public string CashAccountCode { get; set; } = "1100";

    /// <summary>Accounts receivable code prefix (default: 1200)</summary>
    public string ReceivablesAccountCode { get; set; } = "1200";

    /// <summary>Revenue account code prefix (default: 4100)</summary>
    public string RevenueAccountCode { get; set; } = "4100";

    /// <summary>Tax payable account code prefix (default: 2300)</summary>
    public string TaxPayableAccountCode { get; set; } = "2300";

    /// <summary>Retained earnings account code prefix (default: 3300)</summary>
    public string RetainedEarningsAccountCode { get; set; } = "3300";
}
