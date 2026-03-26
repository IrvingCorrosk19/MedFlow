using System.Collections.Concurrent;
using MedFlow.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Middleware;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;
    private static readonly ConcurrentDictionary<string, RateLimitWindow> _windows = new();

    // Limpieza periódica del diccionario para evitar memory leak en producción.
    // El timer es estático: se comparte entre todas las instancias del middleware en el pipeline.
    private static readonly Timer _cleanupTimer = new(
        static _ => CleanupExpiredWindows(),
        state: null,
        dueTime: TimeSpan.FromMinutes(5),
        period: TimeSpan.FromMinutes(5));

    public RateLimitingMiddleware(RequestDelegate next, IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var key = GetRateLimitKey(context, path);

        if (string.IsNullOrEmpty(key))
        {
            await _next(context);
            return;
        }

        var (limit, windowSeconds) = GetLimitForPath(path);
        var window = _windows.GetOrAdd(key, _ => new RateLimitWindow(limit, windowSeconds));

        if (!window.TryAcquire())
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = windowSeconds.ToString();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Too many requests. Please retry later.",
                errorCode = "RATE_LIMIT_EXCEEDED"
            });
            return;
        }

        await _next(context);
    }

    private sealed class RateLimitWindow
    {
        private readonly int _limit;
        private readonly int _windowSeconds;
        private readonly object _lock = new();
        private int _count;
        private DateTimeOffset _windowStart;

        public RateLimitWindow(int limit, int windowSeconds)
        {
            _limit = limit;
            _windowSeconds = windowSeconds;
            _windowStart = DateTimeOffset.UtcNow;
            _count = 1;
        }

        public bool TryAcquire()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                if ((now - _windowStart).TotalSeconds >= _windowSeconds)
                {
                    _windowStart = now;
                    _count = 1;
                    return true;
                }
                if (_count >= _limit)
                    return false;
                _count++;
                return true;
            }
        }

        public bool IsExpired()
        {
            lock (_lock)
            {
                return (DateTimeOffset.UtcNow - _windowStart).TotalSeconds >= _windowSeconds * 2;
            }
        }
    }

    /// <summary>
    /// Elimina ventanas de rate limiting expiradas del diccionario compartido.
    /// Se ejecuta periódicamente para evitar memory leak en producción de larga duración.
    /// </summary>
    private static void CleanupExpiredWindows()
    {
        foreach (var key in _windows.Keys.ToArray())
        {
            if (_windows.TryGetValue(key, out var window) && window.IsExpired())
                _windows.TryRemove(key, out _);
        }
    }

    private static string? GetRateLimitKey(HttpContext context, string path)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
            return $"auth:login:{ip}";
        if (path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase))
            return $"auth:refresh:{ip}";
        if (path.Contains("/webhook", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/stripe", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/n8n", StringComparison.OrdinalIgnoreCase))
            return $"webhook:{ip}";
        if (path.Contains("/api/v1/mobile", StringComparison.OrdinalIgnoreCase))
            return $"mobile:{ip}";
        if (path.Contains("/Onboarding", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase))
            return $"onboarding:{ip}";
        return null;
    }

    private (int limit, int windowSeconds) GetLimitForPath(string path)
    {
        if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
            return (_options.LoginPerMinute, 60);
        if (path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase))
            return (_options.RefreshTokenPerMinute, 60);
        if (path.Contains("/webhook", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/stripe", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/n8n", StringComparison.OrdinalIgnoreCase))
            return (_options.WebhookPerMinute, 60);
        if (path.Contains("/api/v1/mobile", StringComparison.OrdinalIgnoreCase))
            return (_options.ApiPerMinute, 60);
        if (path.Contains("/Onboarding", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase))
            return (_options.OnboardingPerMinute, 60);
        return (100, 60);
    }
}
