namespace MedFlow.Application.Interfaces;

public interface IWorkerHeartbeatService
{
    Task BeatAsync(string workerName, string status = "Running", string? details = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkerHeartbeatVm>> GetRecentAsync(CancellationToken cancellationToken = default);
}

public record WorkerHeartbeatVm(string WorkerName, DateTime LastSeenAt, string Status, string? Details);

public record HealthEntryVm(string Name, string Status);
