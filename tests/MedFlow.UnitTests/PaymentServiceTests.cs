using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for PaymentService covering validation, payment registration, and cancellation.
/// </summary>
public class PaymentServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static PaymentService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        return new PaymentService(
            db,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries: null);
    }

    /// <summary>Seeds a minimal BillingInvoice and the Patient it belongs to.</summary>
    private static async Task<(BillingInvoice Invoice, Patient Patient)> SeedPendingInvoiceAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        decimal totalAmount = 500m,
        InvoiceStatus status = InvoiceStatus.Pending)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PrimerNombre = "Ana",
            PrimerApellido = "García",
            IsActive = true,
            NumeroDocumento = $"DOC-{Guid.NewGuid():N}"[..12]
        };

        var invoice = new BillingInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = patient.Id,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-000001",
            IssueDate = DateTime.UtcNow,
            Subtotal = totalAmount,
            DiscountAmount = 0,
            TaxAmount = 0,
            TotalAmount = totalAmount,
            AmountPaid = 0,
            BalanceDue = totalAmount,
            Status = status
        };

        db.Patients.Add(patient);
        db.BillingInvoices.Add(invoice);
        await db.SaveChangesAsync();

        return (invoice, patient);
    }

    // ── RegisterAsync validation ───────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ZeroAmount_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, amount: 0,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("cero", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_NegativeAmount_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, amount: -50,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("cero", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_InvoiceNotFound_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, amount: 100,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("factura", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_CancelledInvoice_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 300m, InvoiceStatus.Cancelled);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            invoice.Id, patient.Id,
            DateTime.UtcNow, amount: 100,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("cancelad", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_PatientMismatch_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, _) = await SeedPendingInvoiceAsync(db, tenantId, 300m);
        var svc = CreateService(db);

        var differentPatientId = Guid.NewGuid();

        var (payment, error) = await svc.RegisterAsync(
            invoice.Id, differentPatientId,
            DateTime.UtcNow, amount: 100,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("paciente", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_ExceedingBalanceDue_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 200m);
        var svc = CreateService(db);

        // Attempt to pay more than BalanceDue (200m + 1 = 201m)
        var (payment, error) = await svc.RegisterAsync(
            invoice.Id, patient.Id,
            DateTime.UtcNow, amount: 201m,
            ClinicalPaymentMethod.Cash, null, null, null);

        Assert.Null(payment);
        Assert.NotNull(error);
        Assert.Contains("saldo", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_ValidPayment_CreatesPaymentAndCashMovement()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 400m);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            invoice.Id, patient.Id,
            DateTime.UtcNow, amount: 150m,
            ClinicalPaymentMethod.Transfer, "REF-001", "Test payment", "user1");

        Assert.Null(error);
        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(PaymentEntryStatus.Registered, payment.Status);

        // Payment persisted
        var persistedPayment = await db.Payments.FindAsync(payment.Id);
        Assert.NotNull(persistedPayment);

        // CashMovement created
        var cashMovement = await db.CashMovements
            .FirstOrDefaultAsync(c => c.RelatedPaymentId == payment.Id);
        Assert.NotNull(cashMovement);
        Assert.Equal(CashMovementType.Income, cashMovement!.MovementType);
        Assert.Equal(150m, cashMovement.Amount);
    }

    [Fact]
    public async Task RegisterAsync_ValidPayment_UpdatesInvoiceBalanceDue()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 500m);
        var svc = CreateService(db);

        var (payment, error) = await svc.RegisterAsync(
            invoice.Id, patient.Id,
            DateTime.UtcNow, amount: 200m,
            ClinicalPaymentMethod.Cash, null, null, "user1");

        Assert.Null(error);
        Assert.NotNull(payment);

        // Reload the invoice and verify balance was decremented
        var updatedInvoice = await db.BillingInvoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(200m, updatedInvoice!.AmountPaid);
        Assert.Equal(300m, updatedInvoice.BalanceDue);
        Assert.Equal(InvoiceStatus.PartiallyPaid, updatedInvoice.Status);
    }

    // ── CancelPaymentAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CancelPaymentAsync_PaymentNotFound_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db);

        var (ok, error) = await svc.CancelPaymentAsync(Guid.NewGuid(), "user1");

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("pago", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelPaymentAsync_AlreadyCancelled_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 300m);

        // Seed a payment that is already cancelled
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingInvoiceId = invoice.Id,
            PatientId = patient.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = 100m,
            PaymentMethod = ClinicalPaymentMethod.Cash,
            Status = PaymentEntryStatus.Cancelled
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var (ok, error) = await svc.CancelPaymentAsync(payment.Id, "user1");

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("anularse", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelPaymentAsync_ValidPayment_CreatesReversalAndUpdatesInvoice()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var (invoice, patient) = await SeedPendingInvoiceAsync(db, tenantId, 400m);
        var svc = CreateService(db);

        // Register a payment first
        var (payment, _) = await svc.RegisterAsync(
            invoice.Id, patient.Id,
            DateTime.UtcNow, amount: 400m,
            ClinicalPaymentMethod.Cash, null, null, "user1");

        Assert.NotNull(payment);

        // Now cancel it
        var (ok, error) = await svc.CancelPaymentAsync(payment!.Id, "user1");

        Assert.True(ok);
        Assert.Null(error);

        // Payment status updated to Cancelled
        var cancelledPayment = await db.Payments.FindAsync(payment.Id);
        Assert.NotNull(cancelledPayment);
        Assert.Equal(PaymentEntryStatus.Cancelled, cancelledPayment!.Status);

        // Reversal CashMovement created (Expense type)
        var reversal = await db.CashMovements
            .Where(c => c.RelatedPaymentId == payment.Id && c.MovementType == CashMovementType.Expense)
            .FirstOrDefaultAsync();
        Assert.NotNull(reversal);

        // Invoice BalanceDue restored
        var updatedInvoice = await db.BillingInvoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(0m, updatedInvoice!.AmountPaid);
        Assert.Equal(400m, updatedInvoice.BalanceDue);
        Assert.Equal(InvoiceStatus.Pending, updatedInvoice.Status);
    }
}
