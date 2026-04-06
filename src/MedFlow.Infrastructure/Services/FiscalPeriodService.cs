using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class FiscalPeriodService : IFiscalPeriodService
{
    private readonly IApplicationDbContext _context;

    public FiscalPeriodService(IApplicationDbContext context)
    {
        _context = context;
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
        return (true, null);
    }
}
