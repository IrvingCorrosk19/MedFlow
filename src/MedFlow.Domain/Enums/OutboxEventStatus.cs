namespace MedFlow.Domain.Enums;

public enum OutboxEventStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2
}
