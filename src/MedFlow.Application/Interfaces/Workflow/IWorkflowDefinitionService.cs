using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces.Workflow;

public interface IWorkflowDefinitionService
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> CreateAsync(CreateWorkflowDefinitionCommand command, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> UpdateAsync(UpdateWorkflowDefinitionCommand command, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record CreateWorkflowDefinitionCommand(
    string Name,
    string Code,
    string? Description,
    string TriggerEvent,
    string WebhookUrl,
    string HttpMethod = "POST",
    string? HeadersJson = null,
    string PayloadTemplateJson = "{}",
    string? RetryPolicyJson = null,
    int? TimeoutSeconds = null);

public record UpdateWorkflowDefinitionCommand(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string TriggerEvent,
    bool IsActive,
    string WebhookUrl,
    string HttpMethod = "POST",
    string? HeadersJson = null,
    string PayloadTemplateJson = "{}",
    string? RetryPolicyJson = null,
    int? TimeoutSeconds = null);
