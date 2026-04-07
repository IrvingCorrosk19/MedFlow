using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace MedFlow.Infrastructure.Services;

public class JournalEntryService : IJournalEntryService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccountService _accounts;
    private readonly MedFlow.Infrastructure.Persistence.ApplicationDbContext _dbContext;
    private readonly IOptions<AccountMappingOptions> _accountMapping;

    public JournalEntryService(
        IApplicationDbContext context,
        IAccountService accounts,
        MedFlow.Infrastructure.Persistence.ApplicationDbContext dbContext,
        IOptions<AccountMappingOptions> accountMapping)
    {
        _context = context;
        _accounts = accounts;
        _dbContext = dbContext;
        _accountMapping = accountMapping;
    }

    public async Task<IReadOnlyList<JournalEntryListItemDto>> SearchAsync(
        Guid tenantId, DateTime? from, DateTime? to,
        JournalEntryStatus? status, JournalEntryOrigin? origin,
        string? reference, CancellationToken ct = default)
    {
        var q = _context.JournalEntries
            .AsNoTracking()
            .Include(j => j.FiscalPeriod)
            .Where(j => j.TenantId == tenantId)
            .AsQueryable();

        if (from.HasValue) q = q.Where(j => j.EntryDate >= from.Value);
        if (to.HasValue) q = q.Where(j => j.EntryDate <= to.Value);
        if (status.HasValue) q = q.Where(j => j.Status == status.Value);
        if (origin.HasValue) q = q.Where(j => j.Origin == origin.Value);
        if (!string.IsNullOrWhiteSpace(reference)) q = q.Where(j => j.Reference != null && j.Reference.Contains(reference));

        var list = await q.OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.EntryNumber).ToListAsync(ct);

        // Resolve user names
        var userIds = list.Select(j => j.CreatedByUserId).Distinct().ToList();
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName })
            .ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        return list.Select(j => new JournalEntryListItemDto(
            j.Id, j.EntryNumber, j.EntryDate,
            j.FiscalPeriod?.Name ?? "-",
            j.Description, j.Reference, j.Status, j.Origin,
            j.TotalDebit, j.TotalCredit,
            userMap.TryGetValue(j.CreatedByUserId, out var n) ? n : j.CreatedByUserId)).ToList();
    }

    public async Task<JournalEntryDetailDto?> GetDetailAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entry = await _context.JournalEntries
            .AsNoTracking()
            .Include(j => j.FiscalPeriod)
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, ct);

        if (entry is null) return null;

        var user = await _dbContext.Users
            .Where(u => u.Id == entry.CreatedByUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? entry.CreatedByUserId;

        var lines = entry.Lines
            .OrderBy(l => l.LineOrder)
            .Select(l => new JournalEntryLineDto(
                l.Id, l.AccountId,
                l.Account?.Code ?? "-",
                l.Account?.Name ?? "-",
                l.Description, l.Debit, l.Credit, l.LineOrder))
            .ToList();

        return new JournalEntryDetailDto(
            entry.Id, entry.EntryNumber, entry.EntryDate,
            entry.FiscalPeriodId, entry.FiscalPeriod?.Name ?? "-",
            entry.Description, entry.Reference, entry.Status, entry.Origin,
            entry.SourceDocumentId, entry.TotalDebit, entry.TotalCredit,
            user, entry.PostedAt, lines);
    }

    public async Task<JournalEntry?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, ct);

    public async Task<(JournalEntry Entry, string? Error)> CreateAsync(
        Guid tenantId, JournalEntryFormDto form, string userId, CancellationToken ct = default)
    {
        var validationError = ValidateLines(form.Lines);
        if (validationError is not null)
            return (null!, validationError);

        var period = await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == form.FiscalPeriodId, ct);
        if (period is null) return (null!, "Período fiscal no encontrado.");
        if (period.Status == FiscalPeriodStatus.Closed) return (null!, "El período fiscal está cerrado.");

        var entryNumber = await GenerateNextEntryNumberAsync(tenantId, ct);
        var entry = new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = entryNumber,
            FiscalPeriodId = form.FiscalPeriodId,
            EntryDate = form.EntryDate,
            Description = form.Description,
            Reference = form.Reference,
            Status = JournalEntryStatus.Draft,
            Origin = JournalEntryOrigin.Manual,
            CreatedByUserId = userId,
            TotalDebit = form.Lines.Sum(l => l.Debit),
            TotalCredit = form.Lines.Sum(l => l.Credit)
        };

        int order = 1;
        foreach (var line in form.Lines)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                Description = line.Description,
                Debit = line.Debit,
                Credit = line.Credit,
                LineOrder = order++
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
        return (entry, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid id, JournalEntryFormDto form, CancellationToken ct = default)
    {
        // Load without Include to avoid navigation-collection tracking conflicts
        var entry = await _dbContext.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entry is null) return (false, "Asiento no encontrado.");
        if (entry.Status != JournalEntryStatus.Draft) return (false, "Solo se pueden editar asientos en borrador.");

        var validationError = ValidateLines(form.Lines);
        if (validationError is not null) return (false, validationError);

        var period = await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == form.FiscalPeriodId, ct);
        if (period is null) return (false, "Período fiscal no encontrado.");
        if (period.Status == FiscalPeriodStatus.Closed) return (false, "El período fiscal está cerrado.");

        // Delete old lines via a separate tracked query
        var oldLines = await _dbContext.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntryId == id)
            .ToListAsync(ct);
        foreach (var l in oldLines)
            _dbContext.JournalEntryLines.Remove(
                _dbContext.JournalEntryLines.Find(l.Id) ?? l);

        // Update scalar properties
        entry.FiscalPeriodId = form.FiscalPeriodId;
        entry.EntryDate = form.EntryDate;
        entry.Description = form.Description;
        entry.Reference = form.Reference;
        entry.TotalDebit = form.Lines.Sum(l => l.Debit);
        entry.TotalCredit = form.Lines.Sum(l => l.Credit);

        // Add new lines
        int order = 1;
        foreach (var line in form.Lines)
        {
            _dbContext.JournalEntryLines.Add(new JournalEntryLine
            {
                JournalEntryId = entry.Id,
                AccountId = line.AccountId,
                Description = line.Description,
                Debit = line.Debit,
                Credit = line.Credit,
                LineOrder = order++
            });
        }

        await _dbContext.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> PostAsync(Guid id, string userId, CancellationToken ct = default)
    {
        using var activity = MedFlowTelemetry.ActivitySource.StartActivity("JournalEntry.Post");
        activity?.SetTag("entry.id", id.ToString());

        var entry = await _context.JournalEntries.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entry is null) return (false, "Asiento no encontrado.");
        if (entry.Status != JournalEntryStatus.Draft) return (false, "Solo se pueden contabilizar asientos en borrador.");
        if (entry.TotalDebit != entry.TotalCredit) return (false, "El asiento no está balanceado (Débito ≠ Crédito).");

        activity?.SetTag("tenant.id", entry.TenantId.ToString());

        var period = await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == entry.FiscalPeriodId, ct);
        if (period?.Status == FiscalPeriodStatus.Closed) return (false, "El período fiscal está cerrado.");

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAt = DateTime.UtcNow;
        entry.PostedByUserId = userId;

        await _context.SaveChangesAsync(ct);

        // Recalculate balances in background
        await _accounts.RecalculateBalancesAsync(entry.TenantId, ct);

        MedFlowTelemetry.JournalEntriesPosted.Add(1,
            new("origin", entry.Origin.ToString()),
            new("tenant.id", entry.TenantId.ToString()));
        activity?.SetStatus(ActivityStatusCode.Ok);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> VoidAsync(Guid id, string userId, string reason, CancellationToken ct = default)
    {
        var entry = await _context.JournalEntries.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entry is null) return (false, "Asiento no encontrado.");
        if (entry.Status == JournalEntryStatus.Voided) return (false, "El asiento ya fue anulado.");
        if (entry.Origin != JournalEntryOrigin.Manual)
            return (false, "Los asientos automáticos no se pueden anular directamente.");

        entry.Status = JournalEntryStatus.Voided;
        entry.Reference = string.IsNullOrWhiteSpace(entry.Reference)
            ? $"ANULADO: {reason}"
            : $"{entry.Reference} | ANULADO: {reason}";

        await _context.SaveChangesAsync(ct);
        await _accounts.RecalculateBalancesAsync(entry.TenantId, ct);

        return (true, null);
    }

    public async Task<JournalEntry?> CreateFromInvoiceAsync(Guid tenantId, BillingInvoice invoice, string userId, CancellationToken ct = default)
    {
        // Lookup accounts: Cuentas por cobrar and Ingresos
        var mapping = _accountMapping.Value;
        var receivableAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code.StartsWith(mapping.ReceivablesAccountCode), ct);
        var revenueAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code.StartsWith(mapping.RevenueAccountCode), ct);
        var taxAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code.StartsWith(mapping.TaxPayableAccountCode), ct);

        if (receivableAccount is null || revenueAccount is null)
            return null; // Chart of accounts not configured yet

        var period = await GetOrCreatePeriodAsync(tenantId, invoice.IssueDate, ct);
        var entryNumber = await GenerateNextEntryNumberAsync(tenantId, ct);

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = entryNumber,
            FiscalPeriodId = period.Id,
            EntryDate = invoice.IssueDate,
            Description = $"Factura {invoice.InvoiceNumber}",
            Reference = invoice.InvoiceNumber,
            Status = JournalEntryStatus.Posted,
            Origin = JournalEntryOrigin.Invoice,
            SourceDocumentId = invoice.Id,
            CreatedByUserId = userId,
            PostedAt = DateTime.UtcNow,
            PostedByUserId = userId
        };

        // Debit: Accounts Receivable
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = receivableAccount.Id,
            Description = $"Factura {invoice.InvoiceNumber} - {invoice.Patient?.FullName}",
            Debit = invoice.TotalAmount,
            Credit = 0,
            LineOrder = 1
        });

        // Credit: Revenue (subtotal)
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = revenueAccount.Id,
            Description = "Ingreso por servicios médicos",
            Debit = 0,
            Credit = invoice.Subtotal,
            LineOrder = 2
        });

        // Credit: Tax payable (if applicable)
        if (invoice.TaxAmount > 0 && taxAccount is not null)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = taxAccount.Id,
                Description = "Impuesto sobre ventas",
                Debit = 0,
                Credit = invoice.TaxAmount,
                LineOrder = 3
            });
        }

        entry.TotalDebit = entry.Lines.Sum(l => l.Debit);
        entry.TotalCredit = entry.Lines.Sum(l => l.Credit);

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
        await _accounts.RecalculateBalancesAsync(tenantId, ct);
        return entry;
    }

    public async Task<JournalEntry?> CreateFromPaymentAsync(Guid tenantId, Payment payment, string userId, CancellationToken ct = default)
    {
        var mapping = _accountMapping.Value;
        var cashAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code.StartsWith(mapping.CashAccountCode), ct);
        var receivableAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code.StartsWith(mapping.ReceivablesAccountCode), ct);

        if (cashAccount is null || receivableAccount is null)
            return null;

        var period = await GetOrCreatePeriodAsync(tenantId, payment.PaymentDate, ct);
        var entryNumber = await GenerateNextEntryNumberAsync(tenantId, ct);

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = entryNumber,
            FiscalPeriodId = period.Id,
            EntryDate = payment.PaymentDate,
            Description = $"Pago recibido - {payment.BillingInvoice?.InvoiceNumber ?? payment.Id.ToString()[..8]}",
            Reference = payment.ReferenceNumber,
            Status = JournalEntryStatus.Posted,
            Origin = JournalEntryOrigin.Payment,
            SourceDocumentId = payment.Id,
            CreatedByUserId = userId,
            PostedAt = DateTime.UtcNow,
            PostedByUserId = userId,
            TotalDebit = payment.Amount,
            TotalCredit = payment.Amount
        };

        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = cashAccount.Id,
            Description = "Cobro recibido",
            Debit = payment.Amount,
            Credit = 0,
            LineOrder = 1
        });

        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = receivableAccount.Id,
            Description = "Cancelación cuentas por cobrar",
            Debit = 0,
            Credit = payment.Amount,
            LineOrder = 2
        });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
        await _accounts.RecalculateBalancesAsync(tenantId, ct);
        return entry;
    }

    public async Task<string> GenerateNextEntryNumberAsync(Guid tenantId, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"AST-{year}-";
        var last = await _context.JournalEntries
            .Where(j => j.TenantId == tenantId && j.EntryNumber.StartsWith(prefix))
            .OrderByDescending(j => j.EntryNumber)
            .Select(j => j.EntryNumber)
            .FirstOrDefaultAsync(ct);

        var next = last is null ? 1
            : int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;

        return $"{prefix}{next:D6}";
    }

    private async Task<FiscalPeriod> GetOrCreatePeriodAsync(Guid tenantId, DateTime date, CancellationToken ct)
    {
        var existing = await _context.FiscalPeriods
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Year == date.Year && f.Month == date.Month, ct);
        if (existing is not null) return existing;

        var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddSeconds(-1);
        var ci = new System.Globalization.CultureInfo("es-ES");
        var monthName = ci.DateTimeFormat.GetMonthName(date.Month);

        var period = new FiscalPeriod
        {
            TenantId = tenantId,
            Year = date.Year,
            Month = date.Month,
            Name = $"{char.ToUpper(monthName[0])}{monthName[1..]} {date.Year}",
            StartDate = start,
            EndDate = end,
            Status = FiscalPeriodStatus.Open
        };
        _context.FiscalPeriods.Add(period);
        await _context.SaveChangesAsync(ct);
        return period;
    }

    private static string? ValidateLines(IEnumerable<JournalEntryLineFormDto> lines)
    {
        var lineList = lines.ToList();
        if (lineList.Count < 2) return "Un asiento requiere al menos 2 líneas.";

        var totalDebit = lineList.Sum(l => l.Debit);
        var totalCredit = lineList.Sum(l => l.Credit);

        if (totalDebit == 0 && totalCredit == 0) return "El asiento no puede tener todos los montos en cero.";
        if (Math.Round(totalDebit, 2) != Math.Round(totalCredit, 2))
            return $"El asiento no está balanceado. Débito: {totalDebit:N2}, Crédito: {totalCredit:N2}";

        return null;
    }
}
