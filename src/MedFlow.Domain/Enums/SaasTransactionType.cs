namespace MedFlow.Domain.Enums;

public enum SaasTransactionType
{
    SubscriptionCreated = 0,
    SubscriptionRenewed = 1,
    Upgrade = 2,
    Downgrade = 3,
    PaymentSucceeded = 4,
    PaymentFailed = 5,
    Refund = 6,
    Cancellation = 7
}
