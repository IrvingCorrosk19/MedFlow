using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Unit tests for BillingInvoiceService covering creation, totals computation, and cancellation rules.
/// </summary>
public class BillingInvoiceServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static BillingInvoiceService CreateService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        bool hasBillingModule = true)
    {
        var tenantCtx = new Mock<ITenantContext>();
        tenantCtx.Setup(t => t.TenantId).Returns(tenantId);
        tenantCtx.Setup(t => t.IgnoreTenantFilter).Returns(false);

        var planFeatures = new Mock<IPlanFeatureService>();
        planFeatures
            .Setup(p => p.HasBillingModuleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasBillingModule);

        return new BillingInvoiceService(
            db,
            tenantCtx.Object,
            planFeatures.Object,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries: null);
    }

    /// <summary>Builds a minimal valid BillingInvoice header (no items, no invoice number yet).</summary>
    private static BillingInvoice NewInvoiceHeader(Guid tenantId, Guid patientId,
        decimal discount = 0m, decimal tax = 0m)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = patientId,
            IssueDate = DateTime.UtcNow,
            DiscountAmount = discount,
            TaxAmount = tax,
            Status = InvoiceStatus.Pending
        };

    /// <summary>Builds a single valid line item.</summary>
    private static BillingInvoiceItem LineItem(decimal unitPrice = 100m, int qty = 1, decimal discount = 0m)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty, // will be set by service
            ItemType = BillingInvoiceItemType.ConsultationGeneral,
            Description = "Consulta general",
            Quantity = qty,
            UnitPrice = unitPrice,
            DiscountAmount = discount
        };

    /// <summary>Seeds a Patient so ValidateHeaderAsync passes.</summary>
    private static async Task<Patient> SeedPatientAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var patient = SeedData.Patient(tenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NoItems_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var svc = CreateService(db, tenantId);

        var invoice = NewInvoiceHeader(tenantId, patient.Id);

        var (result, error) = await svc.CreateAsync(invoice, Array.Empty<BillingInvoiceItem>());

        Assert.NotNull(error);
        Assert.Contains("concepto", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_InvalidPatient_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = CreateService(db, tenantId);

        // PatientId refers to a patient that does not exist
        var invoice = NewInvoiceHeader(tenantId, Guid.NewGuid());
        var items = new[] { LineItem(200m) };

        var (result, error) = await svc.CreateAsync(invoice, items);

        Assert.NotNull(error);
        Assert.Contains("paciente", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ValidInvoice_PersistsAndGeneratesNumber()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var svc = CreateService(db, tenantId);

        var invoice = NewInvoiceHeader(tenantId, patient.Id);
        var items = new[] { LineItem(300m) };

        var (result, error) = await svc.CreateAsync(invoice, items);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.InvoiceNumber));
        Assert.StartsWith($"INV-{DateTime.UtcNow.Year}-", result.InvoiceNumber);

        // Persisted in db
        var persisted = await db.BillingInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == result.Id);
        Assert.NotNull(persisted);
        Assert.Single(persisted!.Items);
    }

    [Fact]
    public async Task CreateAsync_ValidInvoice_CalculatesTotalsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var svc = CreateService(db, tenantId);

        // Two lines: 200 + 150 = 350 subtotal; discount=50, tax=20 → total=320
        var invoice = NewInvoiceHeader(tenantId, patient.Id, discount: 50m, tax: 20m);
        var items = new[]
        {
            LineItem(unitPrice: 200m, qty: 1),
            LineItem(unitPrice: 150m, qty: 1)
        };

        var (result, error) = await svc.CreateAsync(invoice, items);

        Assert.Null(error);
        Assert.Equal(350m, result.Subtotal);
        Assert.Equal(50m, result.DiscountAmount);
        Assert.Equal(20m, result.TaxAmount);
        Assert.Equal(320m, result.TotalAmount);   // 350 - 50 + 20
        Assert.Equal(0m, result.AmountPaid);
        Assert.Equal(320m, result.BalanceDue);
        Assert.Equal(InvoiceStatus.Pending, result.Status);
    }

    // ── CancelAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_InvoiceWithPayments_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var svc = CreateService(db, tenantId);

        // Create invoice via service so it gets a valid invoice number
        var invoice = NewInvoiceHeader(tenantId, patient.Id);
        var (created, _) = await svc.CreateAsync(invoice, new[] { LineItem(500m) });

        // Simulate a partial payment directly — mark AmountPaid > 0
        var dbInvoice = await db.BillingInvoices.FindAsync(created.Id);
        dbInvoice!.AmountPaid = 100m;
        await db.SaveChangesAsync();

        var (ok, error) = await svc.CancelAsync(created.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("pagos", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ReturnsError()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var svc = CreateService(db, tenantId);

        var invoice = NewInvoiceHeader(tenantId, patient.Id);
        var (created, _) = await svc.CreateAsync(invoice, new[] { LineItem(200m) });

        // Cancel once — should succeed
        var (firstOk, _) = await svc.CancelAsync(created.Id);
        Assert.True(firstOk);

        // Cancel again — should fail
        var (ok, error) = await svc.CancelAsync(created.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("cancelada", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── RecalculateInvoiceTotals (tested indirectly) ──────────────────────────

    [Fact]
    public async Task RecalculateInvoiceTotals_FullPayment_SetsStatusPaid()
    {
        // RecalculateInvoiceTotalsFromPaymentsAsync is internal to MedFlow.Infrastructure.
        // We test it indirectly: register a full payment via PaymentService and verify
        // that the invoice transitions to Paid status.

        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var patient = await SeedPatientAsync(db, tenantId);
        var billingSvc = CreateService(db, tenantId);

        // Create invoice: total = 250m
        var invoice = NewInvoiceHeader(tenantId, patient.Id);
        var (created, _) = await billingSvc.CreateAsync(invoice, new[] { LineItem(250m) });
        Assert.NotNull(created);

        // Register a payment equal to BalanceDue (full payment)
        var paymentSvc = new PaymentService(
            db,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries: null);

        var (payment, error) = await paymentSvc.RegisterAsync(
            created.Id, patient.Id,
            DateTime.UtcNow, amount: 250m,
            ClinicalPaymentMethod.Cash, null, null, "user1");

        Assert.Null(error);
        Assert.NotNull(payment);

        // Invoice should now be Paid
        var updatedInvoice = await db.BillingInvoices.FindAsync(created.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatus.Paid, updatedInvoice!.Status);
        Assert.Equal(250m, updatedInvoice.AmountPaid);
        Assert.Equal(0m, updatedInvoice.BalanceDue);
    }
}
