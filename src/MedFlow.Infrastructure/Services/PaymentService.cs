using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IEventLogService _events;
    private readonly IAuditLogService _audit;

    public PaymentService(ApplicationDbContext db, IEventLogService events, IAuditLogService audit)
    {
        _db = db;
        _events = events;
        _audit = audit;
    }

    private IApplicationDbContext Ctx => _db;

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Payments
            .AsNoTracking()
            .Include(p => p.BillingInvoice)
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> SearchAsync(Guid? invoiceId, Guid? patientId, DateTime? from, DateTime? to, ClinicalPaymentMethod? method, CancellationToken cancellationToken = default)
    {
        var q = _db.Payments
            .AsNoTracking()
            .Include(p => p.BillingInvoice)
            .Include(p => p.Patient)
            .AsQueryable();

        if (invoiceId.HasValue)
            q = q.Where(p => p.BillingInvoiceId == invoiceId.Value);
        if (patientId.HasValue)
            q = q.Where(p => p.PatientId == patientId.Value);
        if (from.HasValue)
            q = q.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue)
            q = q.Where(p => p.PaymentDate <= to.Value);
        if (method.HasValue)
            q = q.Where(p => p.PaymentMethod == method.Value);

        return await q.OrderByDescending(p => p.PaymentDate).ToListAsync(cancellationToken);
    }

    public async Task<(Payment? Payment, string? Error)> RegisterAsync(
        Guid billingInvoiceId,
        Guid patientId,
        DateTime paymentDate,
        decimal amount,
        ClinicalPaymentMethod method,
        string? referenceNumber,
        string? notes,
        string? receivedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return (null, "El monto del pago debe ser mayor que cero.");

        amount = Money.Round(amount);

        Payment? payment = null;
        string? error = null;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var invoice = await _db.BillingInvoices
                    .FirstOrDefaultAsync(i => i.Id == billingInvoiceId, cancellationToken);

                if (invoice == null)
                {
                    error = "Factura no encontrada.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                if (invoice.Status == InvoiceStatus.Cancelled)
                {
                    error = "No se puede pagar una factura cancelada.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                if (invoice.PatientId != patientId)
                {
                    error = "El paciente no coincide con la factura.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                await BillingInvoiceService.RecalculateInvoiceTotalsFromPaymentsAsync(invoice, Ctx, cancellationToken);

                if (amount > invoice.BalanceDue + 0.01m)
                {
                    error = "El monto excede el saldo pendiente de la factura.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                payment = new Payment
                {
                    BillingInvoiceId = billingInvoiceId,
                    PatientId = patientId,
                    PaymentDate = paymentDate.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc)
                        : paymentDate.ToUniversalTime(),
                    Amount = amount,
                    PaymentMethod = method,
                    ReferenceNumber = referenceNumber,
                    Notes = notes,
                    ReceivedByUserId = receivedByUserId,
                    Status = PaymentEntryStatus.Registered
                };

                await _db.Payments.AddAsync(payment, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                await BillingInvoiceService.RecalculateInvoiceTotalsFromPaymentsAsync(invoice, Ctx, cancellationToken);
                _db.BillingInvoices.Update(invoice);
                await _db.SaveChangesAsync(cancellationToken);

                var cash = new CashMovement
                {
                    TenantId = invoice.TenantId,
                    MovementDate = payment.PaymentDate,
                    MovementType = CashMovementType.Income,
                    Amount = payment.Amount,
                    Description = $"Pago factura {invoice.InvoiceNumber}",
                    RelatedPaymentId = payment.Id,
                    CreatedByUserId = receivedByUserId ?? "system"
                };
                await _db.CashMovements.AddAsync(cash, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        if (error != null)
            return (null, error);

        await _events.EnqueueAsync("PaymentRegistered", new
        {
            PaymentId = payment!.Id,
            BillingInvoiceId = billingInvoiceId,
            payment.BillingInvoice?.InvoiceNumber,
            Amount = payment.Amount,
            Method = payment.PaymentMethod,
            PatientId = payment.PatientId
        }, "Payment", payment.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("RegisterPayment", "Billing", nameof(Payment), payment.Id.ToString(),
            $"Pago {payment.Amount:N2}"), cancellationToken);

        return (payment, null);
    }

    public async Task<(bool Ok, string? Error)> CancelPaymentAsync(Guid paymentId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        bool ok = false;
        string? error = null;
        Payment? payment = null;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                payment = await _db.Payments
                    .Include(p => p.CashMovements)
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

                if (payment == null)
                {
                    error = "Pago no encontrado.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                if (payment.Status != PaymentEntryStatus.Registered)
                {
                    error = "El pago ya no puede anularse.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(i => i.Id == payment.BillingInvoiceId, cancellationToken);

                if (invoice == null)
                {
                    error = "Factura no encontrada.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                if (invoice.Status == InvoiceStatus.Cancelled)
                {
                    error = "La factura está cancelada.";
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                payment.Status = PaymentEntryStatus.Cancelled;

                var reversal = new CashMovement
                {
                    TenantId = invoice.TenantId,
                    MovementDate = DateTime.UtcNow,
                    MovementType = CashMovementType.Expense,
                    Amount = payment.Amount,
                    Description = $"Reversión pago factura {invoice.InvoiceNumber}",
                    RelatedPaymentId = payment.Id,
                    CreatedByUserId = actorUserId ?? "system"
                };
                await _db.CashMovements.AddAsync(reversal, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await BillingInvoiceService.RecalculateInvoiceTotalsFromPaymentsAsync(invoice, Ctx, cancellationToken);
                _db.BillingInvoices.Update(invoice);
                await _db.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
                ok = true;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        if (!ok)
            return (false, error);

        await _events.EnqueueAsync("PaymentCancelled", new
        {
            PaymentId = payment!.Id,
            payment.BillingInvoiceId,
            payment.BillingInvoice?.InvoiceNumber,
            Amount = payment.Amount
        }, "Payment", payment.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("CancelPayment", "Billing", nameof(Payment), payment.Id.ToString(),
            "Pago anulado"), cancellationToken);

        return (true, null);
    }
}
