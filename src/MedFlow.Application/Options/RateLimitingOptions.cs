namespace MedFlow.Application.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public bool Enabled { get; set; } = true;
    public int LoginPerMinute { get; set; } = 5;
    public int RefreshTokenPerMinute { get; set; } = 10;
    public int WebhookPerMinute { get; set; } = 60;
    public int ApiPerMinute { get; set; } = 100;
    public int OnboardingPerMinute { get; set; } = 5;
}
