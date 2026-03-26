namespace MedFlow.Application.Options;

public sealed class BillingDunningOptions
{
    public const string SectionName = "Billing:Dunning";

    /// <summary>Días de gracia antes de suspender por impago.</summary>
    public int GracePeriodDays { get; set; } = 7;

    /// <summary>Intentos máximos de cobro antes de marcar uncollectible.</summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
