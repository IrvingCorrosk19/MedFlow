namespace MedFlow.Application.Options;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";
    public bool RequireSignature { get; set; } = true;
    public int MaxReplayAgeMinutes { get; set; } = 15;
}
