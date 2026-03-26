namespace MedFlow.Application.Onboarding;

/// <summary>Entrada atómica para crear clínica, suscripción, admin y configuración inicial.</summary>
public sealed class TenantProvisioningRequest
{
    public string ClinicName { get; init; } = "";
    public string Code { get; init; } = "";
    public string? ClinicEmail { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }

    public Guid SubscriptionPlanId { get; init; }
    public bool StartWithTrial { get; init; } = true;

    public string AdminFirstName { get; init; } = "";
    public string AdminLastName { get; init; } = "";
    public string AdminEmail { get; init; } = "";
    public string AdminPassword { get; init; } = "";

    public string TimeZoneId { get; init; } = "America/Mexico_City";
    public string DateFormat { get; init; } = "dd/MM/yyyy";
    public string Currency { get; init; } = "USD";
    public string Language { get; init; } = "es";
}
