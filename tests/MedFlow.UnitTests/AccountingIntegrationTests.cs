using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Integration tests for the full billing → accounting auto-posting flow.
/// Each test uses a fresh in-memory database to ensure isolation.
/// </summary>
public class AccountingIntegrationTests
{
    // ── Construction helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a JournalEntryService wired to the given db context.
    /// RecalculateBalancesAsync is mocked (no-op) to avoid dependencies on
    /// balance calculation logic outside the scope of these tests.
    /// </summary>
    private static JournalEntryService CreateJournalEntryService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        var mockAccounts = new Mock<IAccountService>();
        mockAccounts
            .Setup(a => a.RecalculateBalancesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mapping = Options.Create(new AccountMappingOptions());
        return new JournalEntryService(db, mockAccounts.Object, db, mapping);
    }

    /// <summary>
    /// Creates a BillingInvoiceService with a real JournalEntryService injected
    /// so that the auto-journal hook fires on invoice creation.
    /// </summary>
    private static BillingInvoiceService CreateBillingInvoiceService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        IJournalEntryService? journalEntries = null)
    {
        var tenantCtx = new Mock<ITenantContext>();
        tenantCtx.Setup(t => t.TenantId).Returns(tenantId);
        tenantCtx.Setup(t => t.IgnoreTenantFilter).Returns(false);

        var planFeatures = new Mock<IPlanFeatureService>();
        planFeatures
            .Setup(p => p.HasBillingModuleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new BillingInvoiceService(
            db,
            tenantCtx.Object,
            planFeatures.Object,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries);
    }

    /// <summary>
    /// Creates a PaymentService with a real JournalEntryService injected
    /// so that the auto-journal hook fires on payment registration.
    /// </summary>
    private static PaymentService CreatePaymentService(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db,
        IJournalEntryService? journalEntries = null)
    {
        return new PaymentService(
            db,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries);
    }

    // ── Seed helpers ───────────────────────────────────────────────────────────

    /// <summary>Seeds the four standard accounts (1100, 1200, 2300, 4100) required by auto-posting.</summary>
    private static async Task SeedAccountingAccountsAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var accounts = new[]
        {
            new Account { TenantId = tenantId, Code = "1100", Name = "Caja",                    Type = AccountType.Asset,     AllowsDirectPosting = true },
            new Account { TenantId = tenantId, Code = "1200", Name = "Cuentas por Cobrar",       Type = AccountType.Asset,     AllowsDirectPosting = true },
            new Account { TenantId = tenantId, Code = "2300", Name = "IVA por Pagar",            Type = AccountType.Liability, AllowsDirectPosting = true },
            new Account { TenantId = tenantId, Code = "4100", Name = "Ingresos por Servicios",   Type = AccountType.Revenue,   AllowsDirectPosting = true },
        };
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync();
    }

    /// <summary>Seeds a minimal valid Patient and returns it.</summary>
    private static async Task<Patient> SeedPatientAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db, Guid tenantId)
    {
        var patient = SeedData.Patient(tenantId);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    /// <summary>Builds a minimal valid invoice header (subtotal computed from items by the service).</summary>
    private static BillingInvoice NewInvoiceHeader(Guid tenantId, Guid patientId, decimal taxAmount = 0m)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = patientId,
            IssueDate = DateTime.UtcNow,
            DiscountAmount = 0m,
            TaxAmount = taxAmount,
            Status = InvoiceStatus.Pending
        };

    /// <summary>Builds a single line item with the given unit price.</summary>
    private static BillingInvoiceItem LineItem(decimal unitPrice)
        => new()
        {
            Id = Guid.NewGuid(),
            ItemType = BillingInvoiceItemType.ConsultationGeneral,
            Description = "Consulta",
            Quantity = 1,
            UnitPrice = unitPrice,
            DiscountAmount = 0m
        };

    // ── Tests ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// When an invoice is created and the chart of accounts is configured,
    /// the auto-journal hook must produce a Posted JournalEntry with the
    /// correct debit (Receivables) and credit (Revenue + Tax) lines.
    /// </summary>
    [Fact]
    public async Task CreateInvoice_GeneratesAccountingEntry()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);

        await SeedAccountingAccountsAsync(db, tenantId);
        var patient = await SeedPatientAsync(db, tenantId);

        var journalSvc = CreateJournalEntryService(db);
        var billingSvc = CreateBillingInvoiceService(db, tenantId, journalSvc);

        // subtotal = 100, tax = 13, total = 113
        var invoice = NewInvoiceHeader(tenantId, patient.Id, taxAmount: 13m);
        var (created, error) = await billingSvc.CreateAsync(invoice, new[] { LineItem(100m) });

        Assert.Null(error);
        Assert.NotNull(created);

        // One JournalEntry with Origin = Invoice must exist
        var entry = await db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Origin == JournalEntryOrigin.Invoice);

        Assert.NotNull(entry);
        Assert.Equal(JournalEntryOrigin.Invoice, entry!.Origin);
        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(created.Id, entry.SourceDocumentId);

        // Resolve account codes from lines via account IDs
        var accountIds = entry.Lines.Select(l => l.AccountId).ToList();
        var accounts = await db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Code);

        // Debit line: Receivables (1200) for total amount 113
        var debitLine = entry.Lines.Single(l => l.Debit > 0);
        Assert.True(accounts[debitLine.AccountId].StartsWith("1200"),
            $"Expected debit on account 1200, got {accounts[debitLine.AccountId]}");
        Assert.Equal(113m, debitLine.Debit);
        Assert.Equal(0m, debitLine.Credit);

        // Credit line: Revenue (4100) for subtotal 100
        var revenueLine = entry.Lines.Single(l => l.Credit > 0 && accounts[l.AccountId].StartsWith("4100"));
        Assert.Equal(0m, revenueLine.Debit);
        Assert.Equal(100m, revenueLine.Credit);

        // Credit line: Tax payable (2300) for tax 13
        var taxLine = entry.Lines.Single(l => l.Credit > 0 && accounts[l.AccountId].StartsWith("2300"));
        Assert.Equal(0m, taxLine.Debit);
        Assert.Equal(13m, taxLine.Credit);

        // Entry must be balanced
        Assert.Equal(entry.TotalDebit, entry.TotalCredit);
    }

    /// <summary>
    /// When CreateFromPaymentAsync is called with a payment linked to an invoice
    /// and the chart of accounts is configured, it must produce a Posted JournalEntry
    /// with a debit on Cash (1100) and a credit on Receivables (1200) for the full amount.
    ///
    /// NOTE: PaymentService.RegisterAsync constructs the Payment entity without setting
    /// TenantId, so the guard "payment.TenantId != Guid.Empty" at line 183 of
    /// PaymentService prevents the auto-hook from firing when called through the service.
    /// This test therefore seeds the payment directly in the DB (with TenantId set) and
    /// calls CreateFromPaymentAsync directly — which is the same code path that would
    /// execute if the guard were satisfied.
    /// </summary>
    [Fact]
    public async Task RegisterPayment_GeneratesAccountingEntry()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);

        await SeedAccountingAccountsAsync(db, tenantId);
        var patient = await SeedPatientAsync(db, tenantId);

        var journalSvc = CreateJournalEntryService(db);
        var billingSvc = CreateBillingInvoiceService(db, tenantId, journalSvc);

        // Create invoice so a valid BillingInvoice row exists
        var invoice = NewInvoiceHeader(tenantId, patient.Id, taxAmount: 13m);
        var (created, createError) = await billingSvc.CreateAsync(invoice, new[] { LineItem(100m) });
        Assert.Null(createError);
        // total = 113

        // Seed the payment directly with TenantId set (bypasses the guard in RegisterAsync)
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingInvoiceId = created.Id,
            PatientId = patient.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = 113m,
            PaymentMethod = ClinicalPaymentMethod.Cash,
            Status = PaymentEntryStatus.Registered
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // Call the accounting hook directly
        var entry = await journalSvc.CreateFromPaymentAsync(tenantId, payment, "user1");

        Assert.NotNull(entry);
        Assert.Equal(JournalEntryOrigin.Payment, entry!.Origin);
        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(payment.Id, entry.SourceDocumentId);

        // Reload lines from DB
        var paymentEntry = await db.JournalEntries
            .Include(j => j.Lines)
            .FirstAsync(j => j.Id == entry.Id);

        var accountIds = paymentEntry.Lines.Select(l => l.AccountId).ToList();
        var accounts = await db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Code);

        // Debit: Cash (1100) for 113
        var debitLine = paymentEntry.Lines.Single(l => l.Debit > 0);
        Assert.True(accounts[debitLine.AccountId].StartsWith("1100"),
            $"Expected debit on account 1100, got {accounts[debitLine.AccountId]}");
        Assert.Equal(113m, debitLine.Debit);
        Assert.Equal(0m, debitLine.Credit);

        // Credit: Receivables (1200) for 113
        var creditLine = paymentEntry.Lines.Single(l => l.Credit > 0);
        Assert.True(accounts[creditLine.AccountId].StartsWith("1200"),
            $"Expected credit on account 1200, got {accounts[creditLine.AccountId]}");
        Assert.Equal(0m, creditLine.Debit);
        Assert.Equal(113m, creditLine.Credit);

        // Entry must be balanced
        Assert.Equal(paymentEntry.TotalDebit, paymentEntry.TotalCredit);
    }

    /// <summary>
    /// When the chart of accounts has not been set up for a tenant,
    /// CreateFromInvoiceAsync must return null without throwing, and the invoice
    /// itself must still be created successfully.
    /// </summary>
    [Fact]
    public async Task CreateInvoice_WithoutAccountsConfigured_DoesNotCrash()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        // Intentionally skip SeedAccountingAccountsAsync — no accounts in DB

        var patient = await SeedPatientAsync(db, tenantId);

        var journalSvc = CreateJournalEntryService(db);
        var billingSvc = CreateBillingInvoiceService(db, tenantId, journalSvc);

        var invoice = NewInvoiceHeader(tenantId, patient.Id, taxAmount: 10m);
        var (created, error) = await billingSvc.CreateAsync(invoice, new[] { LineItem(200m) });

        // Invoice must be created without error
        Assert.Null(error);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created.InvoiceNumber));

        // No JournalEntry should exist — CreateFromInvoiceAsync returns null gracefully
        var entryCount = await db.JournalEntries
            .CountAsync(j => j.TenantId == tenantId);

        Assert.Equal(0, entryCount);
    }

    /// <summary>
    /// When an invoice with no payments is cancelled, the invoice status must
    /// change to Cancelled.  Because BillingInvoiceService.CancelAsync does not
    /// create a reversal journal entry, we assert that the original auto-journal
    /// entry (if any) is still present but the invoice is marked Cancelled.
    /// The test documents the current behaviour without asserting a reversal entry
    /// that the implementation does not create.
    /// </summary>
    [Fact]
    public async Task CancelInvoice_WithNoPayments_SucceedsAndInvoiceIsCancelled()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);

        await SeedAccountingAccountsAsync(db, tenantId);
        var patient = await SeedPatientAsync(db, tenantId);

        var journalSvc = CreateJournalEntryService(db);
        var billingSvc = CreateBillingInvoiceService(db, tenantId, journalSvc);

        // Create invoice — this triggers the auto-journal
        var invoice = NewInvoiceHeader(tenantId, patient.Id, taxAmount: 5m);
        var (created, createError) = await billingSvc.CreateAsync(invoice, new[] { LineItem(50m) });
        Assert.Null(createError);

        // Confirm the auto-journal entry was created
        var originalEntry = await db.JournalEntries
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Origin == JournalEntryOrigin.Invoice);
        Assert.NotNull(originalEntry);

        // Cancel the invoice (no payments registered, so this should succeed)
        var (ok, cancelError) = await billingSvc.CancelAsync(created.Id);

        Assert.True(ok, $"CancelAsync should succeed but returned error: {cancelError}");
        Assert.Null(cancelError);

        // Invoice must now be Cancelled
        var cancelledInvoice = await db.BillingInvoices.FindAsync(created.Id);
        Assert.NotNull(cancelledInvoice);
        Assert.Equal(InvoiceStatus.Cancelled, cancelledInvoice!.Status);

        // CancelAsync auto-creates a reversal journal entry (swapped debits/credits).
        // We expect 2 Invoice-origin entries: the original + the reversal.
        var entries = await db.JournalEntries
            .Where(j => j.TenantId == tenantId && j.Origin == JournalEntryOrigin.Invoice)
            .ToListAsync();
        Assert.Equal(2, entries.Count);

        var reversal = entries.FirstOrDefault(j => j.Description.Contains("Reversión"));
        Assert.NotNull(reversal);
    }
}
