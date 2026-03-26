namespace MedFlow.Application.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public bool RequireHttps { get; set; } = true;
    public int MaxRequestLengthKb { get; set; } = 4096;
    public bool EnableSecurityHeaders { get; set; } = true;
    // Allow known external CDNs used by the Web UI (DataTables/toastr/SweetAlert/Chart.js/Google Fonts).
    // Without these, browsers block the scripts/styles and the UI JS (e.g. DataTable()) fails.
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
        "http://cdn.datatables.net " +
        "https://cdn.datatables.net " +
        "http://cdnjs.cloudflare.com " +
        "https://cdnjs.cloudflare.com " +
        "http://cdn.jsdelivr.net " +
        "https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' " +
        "http://cdn.datatables.net " +
        "https://cdn.datatables.net " +
        "http://cdnjs.cloudflare.com " +
        "https://cdnjs.cloudflare.com " +
        "http://cdn.jsdelivr.net " +
        "https://cdn.jsdelivr.net " +
        "https://fonts.googleapis.com; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data: https:; " +
        "connect-src 'self' " +
        "http://cdn.datatables.net " +
        "https://cdn.datatables.net " +
        "http://cdnjs.cloudflare.com " +
        "https://cdnjs.cloudflare.com " +
        "http://cdn.jsdelivr.net " +
        "https://cdn.jsdelivr.net; " +
        "frame-ancestors 'self';";
    /// <summary>Cookie SameSite: Lax, Strict, or None</summary>
    public string CookieSameSite { get; set; } = "Lax";
    public bool CookieSecure { get; set; }
}
