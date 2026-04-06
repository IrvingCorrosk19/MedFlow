using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class TaxRateService : ITaxRateService
{
    private readonly IApplicationDbContext _context;

    public TaxRateService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaxRateDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var rates = await _context.TaxRates
            .AsNoTracking()
            .Include(t => t.TaxAccount)
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return rates.Select(t => new TaxRateDto(
            t.Id, t.Name, t.Code, t.Rate,
            t.TaxAccountId, t.TaxAccount?.Name,
            t.IsDefault, !t.IsDeleted)).ToList();
    }

    public async Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TaxRates.Include(t => t.TaxAccount).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TaxRate?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.TaxRates.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsDefault, ct);

    public async Task<TaxRate> CreateAsync(TaxRate rate, CancellationToken ct = default)
    {
        if (rate.IsDefault)
            await ClearDefaultsAsync(rate.TenantId, null, ct);

        _context.TaxRates.Add(rate);
        await _context.SaveChangesAsync(ct);
        return rate;
    }

    public async Task UpdateAsync(TaxRate rate, CancellationToken ct = default)
    {
        var existing = await _context.TaxRates.FirstOrDefaultAsync(t => t.Id == rate.Id, ct)
            ?? throw new InvalidOperationException("Tasa no encontrada.");

        if (rate.IsDefault && !existing.IsDefault)
            await ClearDefaultsAsync(existing.TenantId, existing.Id, ct);

        existing.Name = rate.Name;
        existing.Code = rate.Code;
        existing.Rate = rate.Rate;
        existing.TaxAccountId = rate.TaxAccountId;
        existing.IsDefault = rate.IsDefault;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rate = await _context.TaxRates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException("Tasa no encontrada.");
        rate.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    private async Task ClearDefaultsAsync(Guid tenantId, Guid? excludeId, CancellationToken ct)
    {
        var defaults = await _context.TaxRates
            .Where(t => t.TenantId == tenantId && t.IsDefault && (excludeId == null || t.Id != excludeId))
            .ToListAsync(ct);
        foreach (var d in defaults) d.IsDefault = false;
    }
}
