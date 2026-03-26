namespace MedFlow.Application.Interfaces.Workflow;

public interface IWorkflowTestService
{
    Task<WorkflowTestResult> TestWebhookAsync(Guid workflowDefinitionId, CancellationToken cancellationToken = default);
}

public record WorkflowTestResult(bool Success, int? StatusCode, string? ResponseBody, string? ErrorMessage);
