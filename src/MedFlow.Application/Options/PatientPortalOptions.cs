namespace MedFlow.Application.Options;

public class PatientPortalOptions
{
    public bool Enabled { get; set; } = true;
    public bool AllowAppointmentConfirmation { get; set; } = true;
    public bool AllowAppointmentCancellation { get; set; } = true;
    public bool AllowProfileEdit { get; set; } = true;
    public bool ShowMedicalSummary { get; set; } = false;
    public bool ShowPrescriptions { get; set; } = false;
    public bool ShowBilling { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;

    /// <summary>Mostrar recordatorios y preparación antes de la cita (beneficia al paciente y reduce ausencias).</summary>
    public bool ShowAppointmentGuidance { get; set; } = true;
}
