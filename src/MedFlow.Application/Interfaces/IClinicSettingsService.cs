namespace MedFlow.Application.Interfaces;

public sealed record ClinicSettingsDto(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? Timezone,
    string? Currency,
    string? BusinessHoursStart,
    string? BusinessHoursEnd);

public interface IClinicSettingsService
{
    Task<ClinicSettingsDto> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task UpdateAsync(Guid tenantId, ClinicSettingsDto dto, CancellationToken ct = default);
}
