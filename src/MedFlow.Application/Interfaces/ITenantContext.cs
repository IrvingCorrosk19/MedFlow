namespace MedFlow.Application.Interfaces;

/// <summary>Contexto del tenant actual por petición (subdominio, header, usuario).</summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IgnoreTenantFilter { get; }
    string? TenantCode { get; }
    string? TenantName { get; }
    string? LogoUrl { get; }
    string? PrimaryColor { get; }
    string? SecondaryColor { get; }

    void SetResolvedTenant(
        Guid tenantId,
        string code,
        string name,
        string? logoUrl,
        string? primaryColor,
        string? secondaryColor);

    void SetIgnoreTenantFilter(bool ignore);
}
