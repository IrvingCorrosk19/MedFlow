using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Options;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Services;

public class FiscalPeriodService : IFiscalPeriodService
{
    private readonly IApplicationDbContext _context;
    private readonly IOptions<AccountMappingOptions> _accountMapping;
    private readonly ILogger<FiscalPeriodService> _logger;

    public FiscalPeriodService(
        IApplicationDbContext context,
        IOptions<AccountMappingOptions> accountMapping,
        ILogger<FiscalPeriodService> logger)
    {
        _context = context;
        _accountMapping = accountMapping;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FiscalPeriodDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var periods = await _context.FiscalPeriods
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.Year).ThenByDescending(f => f.Month)
            .ToListAsync(ct);

        var counts = await _context.JournalEntries
            .Where(j => j.TenantId == tenantId && j.Status != JournalEntryStatus.Voided)
            .GroupBy(j => j.FiscalPeriodId)
            .Select(g => new { PeriodId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return periods.Select(f =>
        {
            var count = counts.FirstOrDefault(c => c.PeriodId == f.Id)?.Count ?? 0;
            return new FiscalPeriodDto(f.Id, f.Year, f.Month, f.Name, f.StartDate, f.EndDate, f.Status, count, f.IsYearlyClosed);
        }).ToList();
    }

    public async Task<FiscalPeriod?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<FiscalPeriod> GetOrCreateAsync(Guid tenantId, int year, int month, CancellationToken ct = default)
    {
        var existing = await _context.FiscalPeriods
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Year == year && f.Month == month, ct);

        if (existing is not null)
            return existing;

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddSeconds(-1);
        var monthName = new System.Globalization.CultureInfo("es-ES").DateTimeFormat.GetMonthName(month);

        var period = new FiscalPeriod
        {
            TenantId = tenantId,
            Year = year,
            Month = month,
            Name = $"{char.ToUpper(monthName[0])}{monthName[1..]} {year}",
            StartDate = start,
            EndDate = end,
            Status = FiscalPeriodStatus.Open
        };

        _context.FiscalPeriods.Add(period);
        await _context.SaveChangesAsync(ct);
        return period;
    }

    public async Task<(bool Ok, string? Error)> CloseAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var period = await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (period is null) return (false, "Período no encontrado.");
        if (period.Status == FiscalPeriodStatus.Closed) return (false, "El período ya está cerrado.");

        // Ensure no draft entries exist in this period
        var hasDrafts = await _context.JournalEntries
            .AnyAsync(j => j.FiscalPeriodId == id && j.Status == JournalEntryStatus.Draft, ct);
        if (hasDrafts) return (false, "Existen asientos en borrador en este período. Contabilícelos antes de cerrar.");

        period.Status = FiscalPeriodStatus.Closed;
        period.ClosedAt = DateTime.UtcNow;
        period.ClosedByUserId = userId;
        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReopenAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var period = await _context.FiscalPeriods.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (period is null) return (false, "Período no encontrado.");
        if (period.Status == FiscalPeriodStatus.Open) return (false, "El período ya está abierto.");

        period.Status = FiscalPeriodStatus.Open;
        period.ClosedAt = null;
        period.ClosedByUserId = null;
        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CloseYearAsync(Guid tenantId, int year, string userId, CancellationToken ct = default)
    {
        var periods = await _context.FiscalPeriods
            .Where(f => f.TenantId == tenantId && f.Year == year)
            .ToListAsync(ct);

        if (periods.Count == 0)
            return (false, $"No se encontraron períodos fiscales para el año {year}.");

        // Check for draft entries in the year
        var hasDrafts = await _context.JournalEntries
            .AnyAsync(j => j.TenantId == tenantId
                && j.EntryDate.Year == year
                && j.Status == JournalEntryStatus.Draft, ct);
        if (hasDrafts)
            return (false, $"Existen asientos en borrador del año {year}. Contabilícelos o anúlelos antes de cerrar el ejercicio.");

        var alreadyClosed = periods.All(p => p.IsYearlyClosed);
        if (alreadyClosed)
            return (false, $"El ejercicio {year} ya fue cerrado.");

        var now = DateTime.UtcNow;
        foreach (var period in periods)
        {
            if (period.Status == FiscalPeriodStatus.Open)
            {
                var hasDraftsInPeriod = await _context.JournalEntries
                    .AnyAsync(j => j.FiscalPeriodId == period.Id && j.Status == JournalEntryStatus.Draft, ct);
                if (!hasDraftsInPeriod)
                {
                    period.Status = FiscalPeriodStatus.Closed;
                    period.ClosedAt = now;
                    period.ClosedByUserId = userId;
                }
            }
            period.IsYearlyClosed = true;
            period.YearlyClosedAt = now;
        }

        await _context.SaveChangesAsync(ct);

        // Generate year-end closing journal entry
        await GenerateYearEndClosingEntryAsync(tenantId, year, userId, ct);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CreateOpeningEntryAsync(Guid tenantId, int year, string userId, CancellationToken ct = default)
    {
        // 1. Check if opening entry for this year already exists
        var alreadyExists = await _context.JournalEntries
            .AnyAsync(j => j.TenantId == tenantId
                && j.Origin == JournalEntryOrigin.Opening
                && j.EntryDate.Year == year, ct);

        if (alreadyExists)
            return (false, $"Ya existe un asiento de apertura para el año {year}.");

        // 2. Load all accounts with non-zero balance for this tenant
        var accounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId && a.CurrentBalance != 0 && a.AllowsDirectPosting)
            .ToListAsync(ct);

        if (accounts.Count == 0)
            return (false, "No hay saldos en las cuentas para generar el asiento de apertura.");

        // 3. Get or create January fiscal period for target year
        var openingPeriod = await GetOrCreateAsync(tenantId, year, 1, ct);

        // 4. Build journal entry
        var entry = new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = $"APE-{year}-001",
            FiscalPeriodId = openingPeriod.Id,
            EntryDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Description = $"Asiento de apertura {year}",
            Reference = $"APERTURA-{year}",
            Status = JournalEntryStatus.Posted,
            Origin = JournalEntryOrigin.Opening,
            CreatedByUserId = userId,
            PostedAt = DateTime.UtcNow,
            PostedByUserId = userId
        };

        int lineOrder = 1;

        // 5. Asset/Expense/Cost accounts: debit-normal balance → DEBIT line
        //    Liability/Equity/Revenue accounts: credit-normal balance → CREDIT line
        foreach (var acc in accounts)
        {
            bool isDebitNormal = acc.Type == AccountType.Asset
                              || acc.Type == AccountType.Expense
                              || acc.Type == AccountType.Cost;

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = acc.Id,
                Description = $"Saldo inicial: {acc.Name}",
                Debit  = isDebitNormal ? acc.CurrentBalance : 0,
                Credit = isDebitNormal ? 0 : acc.CurrentBalance,
                LineOrder = lineOrder++
            });
        }

        entry.TotalDebit  = entry.Lines.Sum(l => l.Debit);
        entry.TotalCredit = entry.Lines.Sum(l => l.Credit);

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Opening entry {EntryNumber} created for tenant {TenantId}, year {Year}. Lines: {Lines}, TotalDebit: {Debit}",
            entry.EntryNumber, tenantId, year, entry.Lines.Count, entry.TotalDebit);

        return (true, null);
    }

    private async Task GenerateYearEndClosingEntryAsync(Guid tenantId, int year, string userId, CancellationToken ct)
    {
        var mapping = _accountMapping.Value;

        // Load Revenue accounts (type 4) with non-zero balance
        var revenueAccounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId
                && a.Type == AccountType.Revenue
                && a.CurrentBalance != 0)
            .ToListAsync(ct);

        // Load Expense and Cost accounts (types 5 and 6) with non-zero balance
        var expenseAccounts = await _context.Accounts
            .Where(a => a.TenantId == tenantId
                && (a.Type == AccountType.Expense || a.Type == AccountType.Cost)
                && a.CurrentBalance != 0)
            .ToListAsync(ct);

        if (revenueAccounts.Count == 0 && expenseAccounts.Count == 0)
        {
            _logger.LogInformation(
                "Year-end closing: no temporary accounts with non-zero balance for tenant {TenantId}, year {Year}. Skipping closing entry.",
                tenantId, year);
            return;
        }

        // Find Retained Earnings account
        var retainedEarnings = await _context.Accounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId
                && a.Code.StartsWith(mapping.RetainedEarningsAccountCode)
                && a.AllowsDirectPosting, ct);

        if (retainedEarnings is null)
        {
            // Fallback: first active Equity account
            retainedEarnings = await _context.Accounts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId
                    && a.Type == AccountType.Equity
                    && a.AllowsDirectPosting, ct);
        }

        if (retainedEarnings is null)
        {
            _logger.LogWarning(
                "Year-end closing: no Retained Earnings account found for tenant {TenantId}. Closing entry skipped.",
                tenantId);
            return;
        }

        // Calculate totals
        // Revenue accounts have credit-normal balances (CurrentBalance > 0 = credit balance)
        // Expense/Cost accounts have debit-normal balances (CurrentBalance > 0 = debit balance)
        var totalRevenue = revenueAccounts.Sum(a => a.CurrentBalance);
        var totalExpenses = expenseAccounts.Sum(a => a.CurrentBalance);
        var netIncome = totalRevenue - totalExpenses;

        // Get or create a fiscal period for Dec of the closed year for the closing entry
        var closingPeriod = await _context.FiscalPeriods
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Year == year && f.Month == 12, ct);

        if (closingPeriod is null)
        {
            _logger.LogWarning(
                "Year-end closing: no December period found for tenant {TenantId}, year {Year}. Closing entry skipped.",
                tenantId, year);
            return;
        }

        // Build entry number
        var yearPrefix = $"AST-{year}-CIE-";
        var existingClosing = await _context.JournalEntries
            .Where(j => j.TenantId == tenantId && j.EntryNumber.StartsWith(yearPrefix))
            .CountAsync(ct);
        var entryNumber = $"{yearPrefix}{existingClosing + 1:D3}";

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            EntryNumber = entryNumber,
            FiscalPeriodId = closingPeriod.Id,
            EntryDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            Description = $"Asiento de cierre del ejercicio {year}",
            Reference = $"CIERRE-{year}",
            Status = JournalEntryStatus.Posted,
            Origin = JournalEntryOrigin.PeriodClosing,
            CreatedByUserId = userId,
            PostedAt = DateTime.UtcNow,
            PostedByUserId = userId
        };

        int lineOrder = 1;

        // Debit each Revenue account to zero it out (Revenue has credit-normal balance → debit to close)
        foreach (var acc in revenueAccounts)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = acc.Id,
                Description = $"Cierre de ingreso: {acc.Name}",
                Debit = acc.CurrentBalance,
                Credit = 0,
                LineOrder = lineOrder++
            });
        }

        // Credit each Expense/Cost account to zero it out (Expense has debit-normal balance → credit to close)
        foreach (var acc in expenseAccounts)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = acc.Id,
                Description = $"Cierre de gasto/costo: {acc.Name}",
                Debit = 0,
                Credit = acc.CurrentBalance,
                LineOrder = lineOrder++
            });
        }

        // Balancing entry to Retained Earnings
        // Net income > 0 (profit) → Credit retained earnings
        // Net income < 0 (loss)   → Debit retained earnings
        if (netIncome > 0)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = retainedEarnings.Id,
                Description = $"Utilidad del ejercicio {year}",
                Debit = 0,
                Credit = netIncome,
                LineOrder = lineOrder++
            });
        }
        else if (netIncome < 0)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = retainedEarnings.Id,
                Description = $"Pérdida del ejercicio {year}",
                Debit = Math.Abs(netIncome),
                Credit = 0,
                LineOrder = lineOrder++
            });
        }
        else
        {
            // Net income is exactly zero — still need a balancing line if revenue == expenses != 0
            // Only add if there are lines already (to keep the entry valid)
            if (entry.Lines.Count > 0)
            {
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = retainedEarnings.Id,
                    Description = $"Cierre del ejercicio {year} — resultado nulo",
                    Debit = 0,
                    Credit = 0,
                    LineOrder = lineOrder++
                });
            }
        }

        entry.TotalDebit = entry.Lines.Sum(l => l.Debit);
        entry.TotalCredit = entry.Lines.Sum(l => l.Credit);

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(ct);

        // Zero out balances for temporary accounts
        foreach (var acc in revenueAccounts)
            acc.CurrentBalance = 0;
        foreach (var acc in expenseAccounts)
            acc.CurrentBalance = 0;

        // Update retained earnings balance
        retainedEarnings.CurrentBalance += netIncome;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Year-end closing entry {EntryNumber} generated for tenant {TenantId}, year {Year}. Revenue: {Revenue}, Expenses: {Expenses}, Net: {Net}",
            entryNumber, tenantId, year, totalRevenue, totalExpenses, netIncome);
    }
}
