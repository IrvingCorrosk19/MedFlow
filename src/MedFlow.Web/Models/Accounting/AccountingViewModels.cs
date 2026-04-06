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
