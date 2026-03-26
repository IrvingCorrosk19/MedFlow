using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

/// <summary>Registro de webhooks procesados para idempotencia (sin scope de tenant).</summary>
public class StripeWebhookEventLog : BaseEntity
{
    public string ProviderEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? PayloadJson { get; set; }
    public string? ErrorMessage { get; set; }
}
