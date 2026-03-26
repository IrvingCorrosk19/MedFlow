namespace MedFlow.Domain.Entities;

public class PushDeviceToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "Unknown";
    public string? DeviceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
