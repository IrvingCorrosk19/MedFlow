using MedFlow.Application.Options;
using MedFlow.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedFlow.UnitTests;

public class SecurityHeadersMiddlewareTests
{
    private static SecurityHeadersMiddleware Create(bool enabled = true,
        bool requireHttps = false, string? csp = null)
    {
        return new SecurityHeadersMiddleware(_ => Task.CompletedTask);
    }

    private static IOptions<SecurityOptions> Options(bool enabled = true,
        bool requireHttps = false, string? csp = null)
    {
        return Microsoft.Extensions.Options.Options.Create(new SecurityOptions
        {
            EnableSecurityHeaders = enabled,
            RequireHttps = requireHttps,
            ContentSecurityPolicy = csp
        });
    }

    private static DefaultHttpContext HttpContext(bool isHttps = false)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = isHttps ? "https" : "http";
        // Simulate IsHttps for the middleware
        ctx.Request.IsHttps = isHttps;
        return ctx;
    }

    // ── Headers added when enabled ─────────────────────────────────────────

    [Fact]
    public async Task WhenEnabled_AddsXContentTypeOptions()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options());

        Assert.Equal("nosniff", ctx.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task WhenEnabled_AddsXFrameOptions()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options());

        Assert.Equal("SAMEORIGIN", ctx.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task WhenEnabled_AddsReferrerPolicy()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options());

        Assert.Equal("strict-origin-when-cross-origin", ctx.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task WhenEnabled_AddsPermissionsPolicy()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options());

        var pp = ctx.Response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("geolocation=()", pp);
        Assert.Contains("microphone=()", pp);
        Assert.Contains("camera=()", pp);
    }

    // ── CSP ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenEnabled_AndCspConfigured_AddsCspHeader()
    {
        var middleware = Create();
        var ctx = HttpContext();
        var csp = "default-src 'self'; script-src 'self'";

        await middleware.InvokeAsync(ctx, Options(csp: csp));

        Assert.Equal(csp, ctx.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task WhenEnabled_AndNoCsp_NoCspHeaderAdded()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options(csp: null));

        Assert.False(ctx.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    // ── HSTS ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenRequireHttps_AndRequestIsHttps_AddsHsts()
    {
        var middleware = Create();
        var ctx = HttpContext(isHttps: true);

        await middleware.InvokeAsync(ctx, Options(requireHttps: true));

        var hsts = ctx.Response.Headers["Strict-Transport-Security"].ToString();
        Assert.Contains("max-age=31536000", hsts);
        Assert.Contains("includeSubDomains", hsts);
    }

    [Fact]
    public async Task WhenRequireHttps_ButRequestIsHttp_NoHsts()
    {
        var middleware = Create();
        var ctx = HttpContext(isHttps: false);

        await middleware.InvokeAsync(ctx, Options(requireHttps: true));

        Assert.False(ctx.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    // ── Disabled ───────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenDisabled_NoSecurityHeadersAdded()
    {
        var middleware = Create();
        var ctx = HttpContext();

        await middleware.InvokeAsync(ctx, Options(enabled: false));

        Assert.False(ctx.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.False(ctx.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.False(ctx.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.False(ctx.Response.Headers.ContainsKey("Permissions-Policy"));
    }
}
