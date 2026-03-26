namespace MedFlow.Infrastructure.Startup;

public interface IStartupValidator
{
    Task<StartupValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

public sealed record StartupValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string>? CriticalErrors = null)
{
    public bool HasCriticalErrors => CriticalErrors is { Count: > 0 };
}
