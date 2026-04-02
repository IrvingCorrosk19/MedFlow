using MedFlow.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MedFlow.Web.ViewModels;

public class CashMovementFormViewModel
{
    [Required(ErrorMessage = "El tipo de movimiento es obligatorio.")]
    [Display(Name = "Tipo")]
    public CashMovementType MovementType { get; set; }

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
    [Display(Name = "Monto")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [Display(Name = "Fecha")]
    public DateTime MovementDate { get; set; } = DateTime.Now;

    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }
}
