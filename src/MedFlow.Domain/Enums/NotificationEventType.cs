namespace MedFlow.Domain.Enums;

public enum NotificationEventType
{
    AppointmentReminder = 0,
    AppointmentConfirmed = 1,
    AppointmentCancelled = 2,
    PaymentReceiptSent = 3,
    PaymentReminder = 4,
    SubscriptionPaymentFailed = 5,
    SubscriptionPaymentSucceeded = 6,
    TenantSuspendedNotice = 7,
    TenantReactivated = 8,
    TrialEndingReminder = 9,
    Custom = 99
}
