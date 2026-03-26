using System.ComponentModel.DataAnnotations;
using MedFlow.Domain.Enums;

namespace MedFlow.Web.ViewModels;

public class BillingLineInputViewModel
{
    public BillingInvoiceItemType ItemType { get; set; }
    [Required(ErrorMessage = "Descripción requerida")]
    public string Description { get; set; } = string.Empty;
    [Range(1, 99999)]
    public int Quantity { get; set; } = 1;
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    [Range(0, double.MaxValue)]
    public decimal LineDiscount { get; set; }
}

public class BillingInvoiceCreateViewModel
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? AppointmentId { get; set; }
    public Guid? DoctorId { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "Agregue al menos una línea")]
    public List<BillingLineInputViewModel> Lines { get; set; } = [];
}

public class RegisterPaymentViewModel
{
    [Required]
    public Guid BillingInvoiceId { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public ClinicalPaymentMethod PaymentMethod { get; set; } = ClinicalPaymentMethod.Cash;

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class TenantBillingProfileEditViewModel
{
    [EmailAddress]
    [StringLength(256)]
    public string? BillingEmail { get; set; }

    [StringLength(200)]
    public string? LegalName { get; set; }

    [StringLength(50)]
    public string? TaxId { get; set; }

    [StringLength(2)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? StateProvince { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(200)]
    public string? AddressLine1 { get; set; }

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(3)]
    public string PreferredCurrency { get; set; } = "USD";
}
