using MedFlow.Application.Interfaces;

namespace MedFlow.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public bool IgnoreTenantFilter { get; private set; }
    public string? TenantCode { get; private set; }
    public string? TenantName { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? SecondaryColor { get; private set; }

    public void SetResolvedTenant(
        Guid tenantId,
        string code,
        string name,
        string? logoUrl,
        string? primaryColor,
        string? secondaryColor)
    {
        TenantId = tenantId;
        TenantCode = code;
        TenantName = name;
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
    }

    public void SetIgnoreTenantFilter(bool ignore) => IgnoreTenantFilter = ignore;
}
