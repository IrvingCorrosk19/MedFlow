using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces.Workflow;

public interface IWorkflowDispatcher
{
    Task<DispatchResult> DispatchAsync(WorkflowExecution execution, WorkflowDefinition definition, CancellationToken cancellationToken = default);
}

public record DispatchResult(bool Success, int? StatusCode, string? ResponseBody, string? ErrorMessage);
