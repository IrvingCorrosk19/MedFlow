namespace MedFlow.Application.Onboarding;

/// <summary>Claves de configuración por tenant (<c>TenantSettings</c>) usadas en provisionamiento y runtime.</summary>
public static class TenantSettingKeys
{
    public const string CoreTimeZone = "core.timezone";
    public const string CoreDateFormat = "core.dateFormat";
    public const string CoreCurrency = "core.currency";
    public const string CoreLanguage = "core.language";

    public const string BrandingPrimaryColor = "branding.primaryColor";
    public const string BrandingSecondaryColor = "branding.secondaryColor";
    public const string BrandingLogoUrl = "branding.logoUrl";

    /// <summary>JSON array de nombres de especialidades iniciales.</summary>
    public const string CatalogDefaultSpecialities = "catalog.defaultSpecialities";

    // ── Account mapping overrides (per-tenant) ──────────────────────────────
    public const string AccountingCashCode             = "accounting.map.cash";
    public const string AccountingReceivablesCode      = "accounting.map.receivables";
    public const string AccountingRevenueCode          = "accounting.map.revenue";
    public const string AccountingTaxPayableCode       = "accounting.map.taxPayable";
    public const string AccountingRetainedEarningsCode = "accounting.map.retainedEarnings";
}
