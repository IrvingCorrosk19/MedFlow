using MedFlow.Application.Exceptions;
using MedFlow.Application.Options;
using MedFlow.Infrastructure.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace MedFlow.UnitTests;

public class GlobalExceptionHandlingTests
{
    private static GlobalExceptionHandlingMiddleware Create(
        Exception exceptionToThrow,
        bool isDevelopment = false,
        bool includeTrace = false)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName)
           .Returns(isDevelopment ? "Development" : "Production");

        RequestDelegate next = _ => throw exceptionToThrow;

        return new GlobalExceptionHandlingMiddleware(
            next,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance,
            env.Object);
    }

    private static IOptions<ObservabilityOptions> ObsOptions(bool includeTrace = false)
        => Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions
        {
            IncludeTraceInErrorResponse = includeTrace
        });

    private static async Task<(int statusCode, string body)> InvokeAsync(
        GlobalExceptionHandlingMiddleware middleware,
        bool includeTrace = false)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(ctx, ObsOptions(includeTrace));

        ctx.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var body = await new System.IO.StreamReader(ctx.Response.Body, System.Text.Encoding.UTF8).ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    // ── HTTP status code mapping ───────────────────────────────────────────

    [Fact]
    public async Task AppValidationException_Returns400()
    {
        var mw = Create(new AppValidationException(new Dictionary<string, string[]> { ["campo"] = ["Campo inválido."] }));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var mw = Create(new NotFoundException("Paciente", Guid.NewGuid()));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.NotFound, code);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns403()
    {
        var mw = Create(new UnauthorizedAccessException("Acceso denegado."));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.Forbidden, code);
    }

    [Fact]
    public async Task ConflictException_Returns409()
    {
        var mw = Create(new ConflictException("Código en uso.", "CODE_CONFLICT"));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.Conflict, code);
    }

    [Fact]
    public async Task TenantResolutionException_Returns400()
    {
        var mw = Create(new TenantResolutionException("Tenant no encontrado."));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var mw = Create(new InvalidOperationException("Error inesperado."));
        var (code, _) = await InvokeAsync(mw);
        Assert.Equal((int)HttpStatusCode.InternalServerError, code);
    }

    // ── Error code mapping ─────────────────────────────────────────────────

    [Fact]
    public async Task AppValidationException_ResponseContains_VALIDATION_ERROR()
    {
        var mw = Create(new AppValidationException(new Dictionary<string, string[]> { ["correo"] = ["El correo es inválido."] }));
        var (_, body) = await InvokeAsync(mw);
        Assert.Contains("VALIDATION_ERROR", body);
    }

    [Fact]
    public async Task NotFoundException_ResponseContains_NOT_FOUND()
    {
        var mw = Create(new NotFoundException("Paciente", Guid.NewGuid()));
        var (_, body) = await InvokeAsync(mw);
        Assert.Contains("NOT_FOUND", body);
    }

    [Fact]
    public async Task ConflictException_ResponseContains_CustomCode()
    {
        var mw = Create(new ConflictException("Duplicado.", "MY_CONFLICT_CODE"));
        var (_, body) = await InvokeAsync(mw);
        Assert.Contains("MY_CONFLICT_CODE", body);
    }

    // ── Message sanitization in production ────────────────────────────────

    [Fact]
    public async Task InProduction_UnhandledException_DoesNotLeakInternalMessage()
    {
        var mw = Create(new Exception("Detalles internos del servidor."), isDevelopment: false);
        var (_, body) = await InvokeAsync(mw);
        Assert.DoesNotContain("Detalles internos del servidor", body);
        Assert.Contains("error inesperado", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InProduction_ValidationException_IsNotSanitized()
    {
        var errors = new Dictionary<string, string[]> { ["correo"] = ["El correo es inválido."] };
        var mw = Create(new AppValidationException(errors), isDevelopment: false);
        var (_, body) = await InvokeAsync(mw);
        Assert.Contains("VALIDATION_ERROR", body);
    }

    [Fact]
    public async Task InProduction_NotFoundException_ExposesItsMessage()
    {
        var mw = Create(new NotFoundException("Paciente", Guid.NewGuid()), isDevelopment: false);
        var (_, body) = await InvokeAsync(mw);
        Assert.Contains("Paciente", body);
    }

    [Fact]
    public async Task InProduction_TenantResolutionException_IsSanitized()
    {
        var mw = Create(new TenantResolutionException("Subdomain abc not found in DB."), isDevelopment: false);
        var (_, body) = await InvokeAsync(mw);
        Assert.DoesNotContain("Subdomain", body);
        Assert.DoesNotContain("not found in DB", body);
        Assert.Contains("determinar", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InProduction_UnauthorizedAccessException_IsSanitized()
    {
        var mw = Create(new UnauthorizedAccessException("User role X is not in role Y."), isDevelopment: false);
        var (_, body) = await InvokeAsync(mw);
        Assert.DoesNotContain("role X", body);
        Assert.Contains("permiso", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Trace ID ───────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenIncludeTraceEnabled_ResponseContainsTraceId()
    {
        var mw = Create(new Exception("Error"));
        var (_, body) = await InvokeAsync(mw, includeTrace: true);
        Assert.Contains("traceId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhenIncludeTraceDisabled_ResponseDoesNotContainTraceId()
    {
        var mw = Create(new Exception("Error"));
        var (_, body) = await InvokeAsync(mw, includeTrace: false);
        // traceId should be null/absent when disabled
        Assert.DoesNotContain("\"traceId\":\"", body);
    }

    // ── Content-Type ───────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContentType_IsApplicationJson()
    {
        var mw = Create(new Exception("Error"));
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();

        await mw.InvokeAsync(ctx, ObsOptions());

        Assert.Equal("application/json", ctx.Response.ContentType);
    }
}
