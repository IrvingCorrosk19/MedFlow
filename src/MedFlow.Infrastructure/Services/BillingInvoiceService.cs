using MedFlow.Application.Interfaces;
using AuditLogWriteDto = MedFlow.Application.Interfaces.AuditLogWriteDto;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

internal static class Money
{
    public static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

public class BillingInvoiceService : IBillingInvoiceService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly IPlanFeatureService _planFeatures;
    private readonly IEventLogService _events;
    private readonly IAuditLogService _audit;
    private readonly IJournalEntryService? _journalEntries;

    public BillingInvoiceService(
        IApplicationDbContext context,
        ITenantContext tenant,
        IPlanFeatureService planFeatures,
        IEventLogService events,
        IAuditLogService audit,
        IJournalEntryService? journalEntries = null)
    {
        _context = context;
        _tenant = tenant;
        _planFeatures = planFeatures;
        _events = events;
        _audit = audit;
        _journalEntries = journalEntries;
    }

    public async Task<BillingInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BillingInvoices
            .AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.Doctor)
            .Include(i => i.Appointment)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BillingInvoice>> SearchAsync(Guid? patientId, DateTime? from, DateTime? to, InvoiceStatus? status, CancellationToken cancellationToken = default)
    {
        var q = _context.BillingInvoices
            .AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.Doctor)
            .AsQueryable();

        if (patientId.HasValue)
            q = q.Where(i => i.PatientId == patientId.Value);
        if (from.HasValue)
            q = q.Where(i => i.IssueDate >= from.Value);
        if (to.HasValue)
            q = q.Where(i => i.IssueDate <= to.Value);
        if (status.HasValue)
            q = q.Where(i => i.Status == status.Value);

        return await q.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.InvoiceNumber).ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant no resuelto para numeración de facturas.");

        var tenantId = _tenant.TenantId.Value;
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var last = await _context.BillingInvoices
            .Where(i => i.TenantId == tenantId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last != null && last.Length > prefix.Length && int.TryParse(last.AsSpan(prefix.Length), out var n))
            next = n + 1;

        return $"{prefix}{next:D6}";
    }

    public async Task<(BillingInvoice Invoice, string? Error)> CreateAsync(BillingInvoice invoice, IReadOnlyList<BillingInvoiceItem> items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
            return (invoice, "Debe incluir al menos un concepto facturado.");

        if (_tenant.TenantId.HasValue)
        {
            var allowed = await _planFeatures.HasBillingModuleAsync(_tenant.TenantId.Value, cancellationToken);
            if (!allowed)
                return (invoice, "El módulo de facturación no está incluido en su plan actual. Considere actualizar su suscripción.");
        }

        var err = await ValidateHeaderAsync(invoice, cancellationToken);
        if (err != null)
            return (invoice, err);

        foreach (var line in items)
        {
            var le = ValidateAndComputeLine(line);
            if (le != null)
                return (invoice, le);
        }

        ComputeInvoiceTotals(invoice, items);

        if (invoice.TotalAmount < 0)
            return (invoice, "El total de la factura no puede ser negativo.");

        invoice.InvoiceNumber = await GenerateNextInvoiceNumberAsync(cancellationToken);
        invoice.AmountPaid = 0;
        invoice.BalanceDue = invoice.TotalAmount;
        invoice.Status = InvoiceStatus.Pending;

        foreach (var line in items)
        {
            line.Id = Guid.NewGuid();
            line.BillingInvoiceId = invoice.Id;
            invoice.Items.Add(line);
        }

        await _context.BillingInvoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _events.EnqueueAsync("InvoiceCreated", new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.PatientId,
            invoice.AppointmentId,
            invoice.TotalAmount,
            invoice.Status
        }, "BillingInvoice", invoice.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("Create", "Billing", nameof(BillingInvoice), invoice.Id.ToString(),
            $"Factura {invoice.InvoiceNumber}"), cancellationToken);

        // Auto-journal: silently create accounting entry if module is configured
        if (_journalEntries != null && _tenant.TenantId.HasValue)
        {
            try
            {
                await _journalEntries.CreateFromInvoiceAsync(_tenant.TenantId.Value, invoice, "system", cancellationToken);
            }
            catch { /* Contabilidad no configurada aún – ignorar */ }
        }

        return (invoice, null);
    }

    public async Task<(BillingInvoice Invoice, string? Error)> UpdateAsync(BillingInvoice invoice, IReadOnlyList<BillingInvoiceItem> items, CancellationToken cancellationToken = default)
    {
        var existing = await _context.BillingInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);

        if (existing == null)
            return (invoice, "Factura no encontrada.");

        if (existing.Status == InvoiceStatus.Cancelled)
            return (invoice, "No se puede editar una factura cancelada.");

        if (existing.Status == InvoiceStatus.Paid)
            return (invoice, "No se puede editar una factura ya pagada en su totalidad.");

        if (items == null || items.Count == 0)
            return (invoice, "Debe incluir al menos un concepto facturado.");

        if (_tenant.TenantId.HasValue)
        {
            var allowed = await _planFeatures.HasBillingModuleAsync(_tenant.TenantId.Value, cancellationToken);
            if (!allowed)
                return (invoice, "El módulo de facturación no está incluido en su plan actual. Considere actualizar su suscripción.");
        }

        var err = await ValidateHeaderAsync(invoice, cancellationToken);
        if (err != null)
            return (invoice, err);

        foreach (var line in items)
        {
            var le = ValidateAndComputeLine(line);
            if (le != null)
                return (invoice, le);
        }

        existing.PatientId = invoice.PatientId;
        existing.AppointmentId = invoice.AppointmentId;
        existing.DoctorId = invoice.DoctorId;
        existing.IssueDate = invoice.IssueDate;
        existing.DueDate = invoice.DueDate;
        existing.DiscountAmount = invoice.DiscountAmount;
        existing.TaxAmount = invoice.TaxAmount;
        existing.Notes = invoice.Notes;

        _context.BillingInvoiceItems.RemoveRange(existing.Items);
        existing.Items.Clear();

        ComputeInvoiceTotals(existing, items);

        if (existing.TotalAmount < 0)
            return (invoice, "El total de la factura no puede ser negativo.");

        foreach (var line in items)
        {
            line.Id = Guid.NewGuid();
            line.BillingInvoiceId = existing.Id;
            existing.Items.Add(line);
        }

        await RecalculateInvoiceTotalsFromPaymentsAsync(existing, _context, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (existing, null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.BillingInvoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (inv == null)
            return (false, "Factura no encontrada.");

        if (inv.Status == InvoiceStatus.Cancelled)
            return (false, "La factura ya está cancelada.");

        if (inv.AmountPaid > 0)
            return (false, "No se puede cancelar una factura con pagos registrados. Anule los pagos primero.");

        inv.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        await _events.EnqueueAsync("InvoiceCancelled", new
        {
            inv.Id,
            inv.InvoiceNumber,
            inv.PatientId
        }, "BillingInvoice", inv.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("Cancel", "Billing", nameof(BillingInvoice), inv.Id.ToString(),
            $"Factura {inv.InvoiceNumber} cancelada"), cancellationToken);

        // Auto-reverse the accounting journal entry
        if (_journalEntries is not null && _tenant.TenantId.HasValue)
        {
            var userId = "system";
            await _journalEntries.ReverseFromInvoiceCancelAsync(_tenant.TenantId.Value, inv, userId, cancellationToken);
        }

        return (true, null);
    }

    internal static void ComputeInvoiceTotals(BillingInvoice invoice, IReadOnlyList<BillingInvoiceItem> items)
    {
        var sub = items.Sum(x => Money.Round(x.TotalLineAmount));
        invoice.Subtotal = Money.Round(sub);
        invoice.DiscountAmount = Money.Round(Math.Max(0, invoice.DiscountAmount));
        invoice.TaxAmount = Money.Round(Math.Max(0, invoice.TaxAmount));
        invoice.TotalAmount = Money.Round(invoice.Subtotal - invoice.DiscountAmount + invoice.TaxAmount);
    }

    internal static string? ValidateAndComputeLine(BillingInvoiceItem line)
    {
        if (line.Quantity < 1)
            return "La cantidad debe ser al menos 1 en cada línea.";
        if (line.UnitPrice < 0 || line.DiscountAmount < 0)
            return "Precio y descuentos no pueden ser negativos.";
        var raw = line.Quantity * line.UnitPrice - line.DiscountAmount;
        if (raw < 0)
            return "El importe de línea no puede ser negativo.";
        line.TotalLineAmount = Money.Round(raw);
        return null;
    }

    private async Task<string?> ValidateHeaderAsync(BillingInvoice invoice, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.AnyAsync(p => p.Id == invoice.PatientId, cancellationToken);
        if (!patient)
            return "Paciente no válido.";

        if (invoice.AppointmentId.HasValue)
        {
            var apt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == invoice.AppointmentId.Value, cancellationToken);
            if (apt == null)
                return "La cita indicada no existe.";
            if (apt.PatientId != invoice.PatientId)
                return "La cita no corresponde al paciente seleccionado.";
        }

        if (invoice.DoctorId.HasValue)
        {
            var doc = await _context.Doctors.AnyAsync(d => d.Id == invoice.DoctorId.Value, cancellationToken);
            if (!doc)
                return "Doctor no válido.";
        }

        if (invoice.DiscountAmount < 0 || invoice.TaxAmount < 0)
            return "Descuento e impuestos no pueden ser negativos.";

        return null;
    }

    internal static async Task RecalculateInvoiceTotalsFromPaymentsAsync(BillingInvoice inv, IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var paid = await context.Payments
            .Where(p => p.BillingInvoiceId == inv.Id && p.Status == PaymentEntryStatus.Registered)
            .SumAsync(p => p.Amount, cancellationToken);

        inv.AmountPaid = Money.Round(paid);
        inv.BalanceDue = Money.Round(Math.Max(0, inv.TotalAmount - inv.AmountPaid));

        if (inv.Status == InvoiceStatus.Cancelled)
            return;

        if (inv.TotalAmount <= 0)
        {
            inv.Status = InvoiceStatus.Paid;
            inv.BalanceDue = 0;
            return;
        }

        if (inv.BalanceDue <= 0)
            inv.Status = InvoiceStatus.Paid;
        else if (inv.AmountPaid > 0)
            inv.Status = InvoiceStatus.PartiallyPaid;
        else
            inv.Status = InvoiceStatus.Pending;
    }

}
