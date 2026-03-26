namespace MedFlow.Application.Common;

public sealed record ApiErrorResponse(
    bool Success,
    string Message,
    string? ErrorCode,
    string? TraceId,
    DateTime Timestamp,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    object? Details = null)
{
    public static ApiErrorResponse Create(string message, string? errorCode, string? traceId,
        IReadOnlyDictionary<string, string[]>? validationErrors = null, object? details = null) =>
        new(false, message, errorCode, traceId, DateTime.UtcNow, validationErrors, details);
}
