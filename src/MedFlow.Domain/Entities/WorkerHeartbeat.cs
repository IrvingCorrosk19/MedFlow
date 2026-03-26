namespace MedFlow.Domain.Entities;

public class WorkerHeartbeat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkerName { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; }
    public string Status { get; set; } = "Running";
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
