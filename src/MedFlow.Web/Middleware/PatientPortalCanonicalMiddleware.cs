using System.Text.RegularExpressions;

namespace MedFlow.Web.Middleware;

/// <summary>
/// Redirige rutas legacy área <c>/PatientPortal/*</c> (español / MVC viejo) al portal canónico <c>/portal/*</c>.
/// No intercepta login/logout/acceso denegado para no romper flujo Auth del área.
/// </summary>
public sealed class PatientPortalCanonicalMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, string> ExactRedirects =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["/PatientPortal"] = "/portal/dashboard",
            ["/PatientPortal/"] = "/portal/dashboard",
            ["/PatientPortal/inicio"] = "/portal/dashboard",
            ["/PatientPortal/Home"] = "/portal/dashboard",
            ["/PatientPortal/Home/"] = "/portal/dashboard",
            ["/PatientPortal/citas"] = "/portal/appointments",
            ["/PatientPortal/citas/historial"] = "/portal/appointments",
            ["/PatientPortal/facturas"] = "/portal/invoices",
            ["/PatientPortal/pagos"] = "/portal/invoices",
            ["/PatientPortal/estado-cuenta"] = "/portal/invoices",
            ["/PatientPortal/notificaciones"] = "/portal/notifications",
            ["/PatientPortal/perfil"] = "/portal/profile",
            ["/PatientPortal/perfil/cambiar-contrasena"] = "/portal/change-password",
        };

    private static readonly Regex RxCitaDetail = new(
        @"^/PatientPortal/citas/([0-9a-fA-F-]{36})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RxFacturaDetail = new(
        @"^/PatientPortal/facturas/([0-9a-fA-F-]{36})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RxHistorialMedico = new(
        @"^/PatientPortal/historial-medico(?:/([0-9a-fA-F-]{36}))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public PatientPortalCanonicalMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
            return _next(context);

        var raw = context.Request.Path.Value ?? "";
        var p = raw.TrimEnd('/');
        if (p.Length == 0) p = "/";

        if (p.StartsWith("/PatientPortal/login", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/PatientPortal/logout", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/PatientPortal/acceso-denegado", StringComparison.OrdinalIgnoreCase))
        {
            return _next(context);
        }

        if (ExactRedirects.TryGetValue(p.TrimEnd('/'), out var target)
            || ExactRedirects.TryGetValue(p, out target))
        {
            context.Response.Redirect(target);
            return Task.CompletedTask;
        }

        var mCita = RxCitaDetail.Match(p);
        if (mCita.Success)
        {
            context.Response.Redirect("/portal/appointments/" + mCita.Groups[1].Value);
            return Task.CompletedTask;
        }

        var mFac = RxFacturaDetail.Match(raw);
        if (mFac.Success)
        {
            context.Response.Redirect("/portal/invoices/" + mFac.Groups[1].Value);
            return Task.CompletedTask;
        }

        // Sin vista equivalente única: historial médico legacy → dashboard paciente.
        if (RxHistorialMedico.IsMatch(raw))
        {
            context.Response.Redirect("/portal/dashboard");
            return Task.CompletedTask;
        }

        return _next(context);
    }
}
