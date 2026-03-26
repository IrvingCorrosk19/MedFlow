using MedFlow.Application.Notifications;

namespace MedFlow.Application.Interfaces;

public interface INotificationDispatchService
{
    Task<DispatchResult> DispatchAsync(DispatchRequest request, CancellationToken cancellationToken = default);
}

public sealed record DispatchResult(
    bool Success,
    IReadOnlyList<Guid> MessageIds,
    IReadOnlyList<string> Errors);
