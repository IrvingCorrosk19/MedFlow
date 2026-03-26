using MedFlow.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IOptions<SecurityOptions> options)
    {
        if (!options.Value.EnableSecurityHeaders)
        {
            await _next(context);
            return;
        }

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        if (!string.IsNullOrEmpty(options.Value.ContentSecurityPolicy))
            context.Response.Headers["Content-Security-Policy"] = options.Value.ContentSecurityPolicy;

        if (context.Request.IsHttps && options.Value.RequireHttps)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        await _next(context);
    }
}
