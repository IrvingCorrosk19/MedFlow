namespace MedFlow.Application.Options;

public sealed class SaasOptions
{
    public const string SectionName = "Saas";

    /// <summary>Si es false, el tenant en PastDue no podrá operar (solo pantalla bloqueada).</summary>
    public bool AllowOperationsWhenPastDue { get; set; } = true;

    /// <summary>Rutas permitidas cuando el tenant o la suscripción están suspendidos (acceso administrativo mínimo).</summary>
    public string[] SuspendedTenantAllowedPathPrefixes { get; set; } =
    [
        "/Dashboard",
        "/Settings",
        "/AdminUsers",
        "/Commercial",
        "/Identity"
    ];
}
