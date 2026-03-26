namespace MedFlow.Application.Options;

public sealed class StripeBillingOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
}
