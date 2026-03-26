namespace MedFlow.Infrastructure.Workflow;

public static class WebhookUrlValidator
{
    private static readonly string[] BlockedHosts = ["localhost", "127.0.0.1", "0.0.0.0", "[::1]"];

    public static (bool IsValid, string? Error) Validate(string? url, bool requireHttpsInProduction = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "La URL del webhook es obligatoria");

        url = url.Trim();
        if (url.Length > 2048)
            return (false, "La URL excede la longitud máxima permitida");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
            return (false, "Formato de URL inválido");

        // Comparar el esquema del Uri (p.ej. "https"), no la URL completa — la regex https?:// no aplica a uri.Scheme.
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return (false, "Solo se permiten URLs con esquema HTTP o HTTPS");

        var host = (uri.Host ?? "").ToLowerInvariant();
        if (requireHttpsInProduction)
        {
            if (BlockedHosts.Contains(host))
                return (false, "No se permiten URLs locales o de loopback en producción");
            if (host.StartsWith("192.168.") || host.StartsWith("10.") || host.StartsWith("172."))
                return (false, "No se permiten URLs de red privada en producción");
            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return (false, "En producción se requiere HTTPS para webhooks");
        }

        return (true, null);
    }
}
