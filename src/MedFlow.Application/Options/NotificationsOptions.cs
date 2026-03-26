namespace MedFlow.Application.Options;

public class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public string? N8nWebhookBaseUrl { get; set; }
    public string? N8nApiKey { get; set; }
    public int MaxRetries { get; set; } = 3;
}
