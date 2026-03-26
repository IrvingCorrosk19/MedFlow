using MedFlow.Application.Interfaces;

namespace MedFlow.Infrastructure.Tenancy;

/// <summary>Migraciones y diseño: sin filtro de tenant.</summary>
public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid? TenantId => null;
    public bool IgnoreTenantFilter => true;
    public string? TenantCode => null;
    public string? TenantName => null;
    public string? LogoUrl => null;
    public string? PrimaryColor => null;
    public string? SecondaryColor => null;

    public void SetResolvedTenant(
        Guid tenantId,
        string code,
        string name,
        string? logoUrl,
        string? primaryColor,
        string? secondaryColor)
    {
    }

    public void SetIgnoreTenantFilter(bool ignore)
    {
    }
}
