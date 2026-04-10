namespace MedFlow.Domain;

public static class WorkflowTriggerEvents
{
    public const string AppointmentCreated = "AppointmentCreated";
    public const string AppointmentReminder = "AppointmentReminder";
    public const string AppointmentConfirmed = "AppointmentConfirmed";
    public const string AppointmentCancelled = "AppointmentCancelled";
    public const string PaymentRegistered = "PaymentRegistered";
    public const string InvoiceCreated = "InvoiceCreated";
    public const string InvoicePending = "InvoicePending";
    public const string SaaSPaymentFailed = "SaaSPaymentFailed";
    public const string SaaSPaymentSucceeded = "SaaSPaymentSucceeded";
    public const string TenantSuspended = "TenantSuspended";
    public const string PatientRegistered = "PatientRegistered";
    public const string PatientInactive = "PatientInactive";
    public const string PatientCreated = "PatientCreated";
    public const string UserCreated = "UserCreated";
    public const string InvoiceCancelled = "InvoiceCancelled";
    public const string PaymentCancelled = "PaymentCancelled";
    public const string DailySummary = "DailySummary";
    public const string AIInsightGenerated = "AIInsightGenerated";
    public const string AIRecommendationApplied = "AIRecommendationApplied";
    public const string AIAlertAcknowledged = "AIAlertAcknowledged";
    public const string NoShowRiskDetected = "NoShowRiskDetected";
    public const string PaymentRiskDetected = "PaymentRiskDetected";
    public const string PatientReengagementSuggested = "PatientReengagementSuggested";
    public const string MedicalRecordCreated = "MedicalRecordCreated";
    public const string PrescriptionIssued = "PrescriptionIssued";
    public const string AppointmentRequestedFromPortal = "AppointmentRequestedFromPortal";
    public const string InvoicePaid = "InvoicePaid";
    public const string InvoiceOverdue = "InvoiceOverdue";

    public static IReadOnlyList<string> All { get; } =
    [
        AppointmentCreated,
        AppointmentReminder,
        AppointmentConfirmed,
        AppointmentCancelled,
        AppointmentRequestedFromPortal,
        PaymentRegistered,
        InvoiceCreated,
        InvoicePending,
        InvoicePaid,
        InvoiceOverdue,
        InvoiceCancelled,
        PaymentCancelled,
        SaaSPaymentFailed,
        SaaSPaymentSucceeded,
        TenantSuspended,
        PatientRegistered,
        PatientInactive,
        PatientCreated,
        MedicalRecordCreated,
        PrescriptionIssued,
        UserCreated,
        DailySummary,
        AIInsightGenerated,
        AIRecommendationApplied,
        AIAlertAcknowledged,
        NoShowRiskDetected,
        PaymentRiskDetected,
        PatientReengagementSuggested
    ];
}
