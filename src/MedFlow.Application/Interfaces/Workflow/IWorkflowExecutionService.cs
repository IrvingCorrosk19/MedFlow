using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces.Workflow;

public interface IWorkflowExecutionService
{
    Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowExecution>> ListAsync(WorkflowExecutionListFilter filter, CancellationToken cancellationToken = default);
    Task<WorkflowMetrics> GetMetricsAsync(WorkflowMetricsFilter? filter = null, CancellationToken cancellationToken = default);
    Task RetryAsync(Guid id, CancellationToken cancellationToken = default);
}

public record WorkflowExecutionListFilter(
    Guid? WorkflowDefinitionId = null,
    WorkflowExecutionStatus? Status = null,
    string? EventType = null,
    int Page = 1,
    int PageSize = 50);

public record WorkflowMetricsFilter(
    Guid? WorkflowDefinitionId = null,
    DateTime? From = null,
    DateTime? To = null);

public record WorkflowMetrics(
    int TotalExecuted,
    int Succeeded,
    int Failed,
    int Pending,
    int Retrying,
    double AverageExecutionSeconds,
    IReadOnlyList<WorkflowErrorSummary> TopErrors);

public record WorkflowErrorSummary(string Message, int Count);
