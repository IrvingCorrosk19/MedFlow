using System.Net;
using System.Text.Json;
using MedFlow.Application.Common;
using MedFlow.Application.Exceptions;
using MedFlow.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFlow.Infrastructure.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ObservabilityOptions> observability)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, observability.Value);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, ObservabilityOptions observability)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, errorCode, message) = MapException(ex);

        _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}, StatusCode={StatusCode}", traceId, (int)statusCode);

        if (!_env.IsDevelopment())
        {
            message = GetSafeMessage(ex, message);
        }

        // Respetar la configuración Observability:IncludeTraceInErrorResponse.
        var responseTraceId = observability.IncludeTraceInErrorResponse ? traceId : null;

        var response = ApiErrorResponse.Create(
            message,
            errorCode,
            responseTraceId,
            ex is AppValidationException ve ? ve.Errors : null);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    /// <summary>
    /// Determina el mensaje seguro a exponer al cliente según el tipo de excepción.
    /// En producción solo se exponen mensajes diseñados para ser user-facing.
    /// AppValidationException y NotFoundException tienen mensajes seguros por contrato.
    /// ConflictException id. Los demás se sanitizan para no filtrar detalles internos.
    /// </summary>
    private static string GetSafeMessage(Exception ex, string originalMessage) => ex switch
    {
        AppValidationException => originalMessage,   // Mensajes de validación: siempre seguros.
        NotFoundException => originalMessage,        // "X no encontrado": mensaje diseñado para usuario.
        ConflictException => originalMessage,        // "Código en uso": mensaje diseñado para usuario.
        TenantResolutionException => "No se pudo determinar la clínica.",   // Nunca exponer detalles del resolver.
        UnauthorizedAccessException => "No tienes permiso para realizar esta acción.",  // No exponer razón interna.
        _ => "Se produjo un error inesperado. Inténtalo de nuevo más tarde."
    };

    private static (HttpStatusCode statusCode, string? errorCode, string message) MapException(Exception ex)
    {
        return ex switch
        {
            AppValidationException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN", ex.Message),
            NotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", ex.Message),
            ConflictException c => (HttpStatusCode.Conflict, c.Code ?? "CONFLICT", ex.Message),
            TenantResolutionException => (HttpStatusCode.BadRequest, "TENANT_RESOLUTION", ex.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", ex.Message)
        };
    }
}
