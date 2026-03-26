namespace MedFlow.Application.Options;

public sealed class OnboardingOptions
{
    public const string SectionName = "Onboarding";

    /// <summary>Si es false, el flujo público de alta de clínica queda deshabilitado (404).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Si es true, tras el alta se inicia sesión automáticamente (mismo host; el tenant se resuelve por subdominio/header por defecto).</summary>
    public bool AutoSignInAfterProvision { get; set; }
}
