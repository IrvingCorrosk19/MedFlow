namespace MedFlow.Web.Models.Onboarding;

/// <summary>Estado del wizard (sin contraseña en claro; payload protegido tras paso 3).</summary>
public sealed class OnboardingSessionState
{
    public string ClinicName { get; set; } = "";
    public string Code { get; set; } = "";
    public string? ClinicEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public Guid? SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public string? SubscriptionPlanCode { get; set; }
    public bool StartWithTrial { get; set; } = true;

    public string AdminFirstName { get; set; } = "";
    public string AdminLastName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    /// <summary>Data protection (no almacenar contraseña en claro).</summary>
    public string? ProtectedPasswordPayload { get; set; }

    public string TimeZoneId { get; set; } = "America/Mexico_City";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Currency { get; set; } = "USD";
    public string Language { get; set; } = "es";
}
