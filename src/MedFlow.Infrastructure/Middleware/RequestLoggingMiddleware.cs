using System.Diagnostics;
using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext? tenantContext = null)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var correlationId = context.TraceIdentifier;
        var tenantId = tenantContext?.TenantId;
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            if (_logger.IsEnabled(LogLevel.Information) && !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Request {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. CorrelationId={CorrelationId} TenantId={TenantId} UserId={UserId}",
                    method, path, context.Response.StatusCode, sw.ElapsedMilliseconds,
                    correlationId, tenantId, userId != null ? MaskSensitive(userId, 8) : null);
            }
        }
    }

    private static string MaskSensitive(string value, int visible)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visible) return "***";
        return value[..visible] + "***";
    }
}
