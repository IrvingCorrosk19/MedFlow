namespace MedFlow.Domain.Enums;

public enum WorkflowExecutionStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Retrying = 4,
    Cancelled = 5
}
