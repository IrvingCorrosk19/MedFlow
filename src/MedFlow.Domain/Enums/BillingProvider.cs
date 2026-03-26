namespace MedFlow.Domain.Enums;

public enum BillingProvider
{
    None = 0,
    Manual = 1,
    Stripe = 2,
    Paddle = 3,
    Other = 99
}
