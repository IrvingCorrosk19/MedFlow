using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class ClinicSettingsService : IClinicSettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _audit;

    public ClinicSettingsService(IApplicationDbContext context, IAuditLogService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<ClinicSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null)
            return new ClinicSettingsDto("", null, null, null, null, null, null, null, null, null, "USD", "08:00", "17:00");

        var settings = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && s.SettingKey.StartsWith("clinic."))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "", ct);

        string Get(string key, string def = "") =>
            settings.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : def;

        return new ClinicSettingsDto(
            tenant.Name,
            tenant.LegalName,
            tenant.TaxId,
            tenant.Email,
            tenant.Phone,
            tenant.Address,
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.SecondaryColor,
            Get("clinic.timezone"),
            Get("clinic.currency", "USD"),
            Get("clinic.hours.start", "08:00"),
            Get("clinic.hours.end", "17:00"));
    }

    public async Task UpdateAsync(Guid tenantId, ClinicSettingsDto dto, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null) return;

        tenant.Name = dto.Name;
        tenant.LegalName = dto.LegalName;
        tenant.TaxId = dto.TaxId;
        tenant.Email = dto.Email;
        tenant.Phone = dto.Phone;
        tenant.Address = dto.Address;
        tenant.LogoUrl = dto.LogoUrl;
        tenant.PrimaryColor = string.IsNullOrWhiteSpace(dto.PrimaryColor) ? null : dto.PrimaryColor;
        tenant.SecondaryColor = string.IsNullOrWhiteSpace(dto.SecondaryColor) ? null : dto.SecondaryColor;

        var extraKeys = new[]
        {
            ("clinic.timezone",    dto.Timezone ?? ""),
            ("clinic.currency",    dto.Currency ?? "USD"),
            ("clinic.hours.start", dto.BusinessHoursStart ?? "08:00"),
            ("clinic.hours.end",   dto.BusinessHoursEnd ?? "17:00"),
        };

        var keyList = extraKeys.Select(k => k.Item1).ToList();
        var existing = await _context.TenantSettings
            .Where(s => s.TenantId == tenantId && keyList.Contains(s.SettingKey))
            .ToDictionaryAsync(s => s.SettingKey, ct);

        foreach (var (key, value) in extraKeys)
        {
            if (existing.TryGetValue(key, out var s))
            {
                s.SettingValue = value;
                s.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TenantSettings.Add(new TenantSettings
                {
                    TenantId = tenantId,
                    SettingKey = key,
                    SettingValue = value,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLogWriteDto(
            "Update", "Settings", "ClinicSettings", tenantId.ToString(),
            "Configuración de clínica actualizada", null, null), ct);
    }
}
