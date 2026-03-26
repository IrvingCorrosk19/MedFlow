using MedFlow.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IOptions<ObservabilityOptions> options)
    {
        var headerName = options.Value.CorrelationIdHeader;
        if (!context.Request.Headers.TryGetValue(headerName, out var existing))
        {
            var correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[headerName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[headerName] = correlationId;
                return Task.CompletedTask;
            });
            context.TraceIdentifier = correlationId;
        }
        else
        {
            context.TraceIdentifier = existing.ToString();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[headerName] = existing;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
