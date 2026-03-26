using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class PaymentRiskService : IPaymentRiskService
{
    private readonly IApplicationDbContext _context;

    public PaymentRiskService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentRiskResult> EvaluatePatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var factors = new List<string>();
        var score = 0m;

        var pendingInvoices = await _context.BillingInvoices
            .Where(i => i.PatientId == patientId && i.BalanceDue > 0 && i.Status != InvoiceStatus.Cancelled && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        var overdueInvoices = pendingInvoices.Where(i => i.DueDate.HasValue && i.DueDate.Value.Date < now).ToList();
        var totalOverdue = overdueInvoices.Sum(i => i.BalanceDue);
        var totalPending = pendingInvoices.Sum(i => i.BalanceDue);

        var overdueCount = overdueInvoices.Count;
        if (overdueCount > 0)
        {
            score += Math.Min(overdueCount * 15, 45);
            factors.Add($"{overdueCount} factura(s) vencida(s)");
        }

        if (totalOverdue > 0)
        {
            var amountScore = totalOverdue >= 500 ? 25 : totalOverdue >= 200 ? 15 : totalOverdue >= 50 ? 8 : 3;
            score += amountScore;
            factors.Add($"Saldo vencido: ${totalOverdue:F2}");
        }

        var payments = await _context.Payments
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        var invoicesWithPayments = await _context.BillingInvoices
            .Include(i => i.Payments)
            .Where(i => i.PatientId == patientId && i.Status == InvoiceStatus.PartiallyPaid && !i.IsDeleted)
            .CountAsync(cancellationToken);
        if (invoicesWithPayments >= 2)
        {
            score += 10;
            factors.Add("Historial de pagos parciales frecuentes");
        }

        int daysAvgOverdue = 0;
        if (overdueInvoices.Count > 0)
        {
            daysAvgOverdue = (int)overdueInvoices.Average(i => (now - (i.DueDate ?? i.IssueDate).Date).TotalDays);
            if (daysAvgOverdue > 30)
            {
                score += 15;
                factors.Add($"Mora promedio: {daysAvgOverdue} días");
            }
            else if (daysAvgOverdue > 14)
            {
                score += 8;
                factors.Add($"Mora promedio: {daysAvgOverdue} días");
            }
        }

        score = Math.Min(score, 100);

        var severity = score >= 60 ? "Critical" : score >= 35 ? "Warning" : "Info";
        var summary = overdueCount > 0
            ? $"Riesgo de cobro: {overdueCount} factura(s) vencida(s), ${totalOverdue:F2} pendiente."
            : pendingInvoices.Count > 0
                ? $"{pendingInvoices.Count} factura(s) pendiente(s), ${totalPending:F2}."
                : "Sin deuda pendiente.";

        var recommendation = score >= 60
            ? "Gestión prioritaria de cobranza. Contacto directo recomendado."
            : score >= 35
                ? "Seguimiento de cobranza. Activar recordatorios."
                : "Monitoreo rutinario.";

        return new PaymentRiskResult(
            score,
            severity,
            summary,
            recommendation,
            factors,
            overdueCount,
            totalOverdue,
            daysAvgOverdue);
    }
}
