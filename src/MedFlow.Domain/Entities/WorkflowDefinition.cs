using MedFlow.Domain.Common;

namespace MedFlow.Domain.Entities;

public class WorkflowDefinition : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerEvent { get; set; } = string.Empty;
    public new bool IsActive { get; set; } = true;
    public string WebhookUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string? HeadersJson { get; set; }
    public string PayloadTemplateJson { get; set; } = "{}";
    public string? RetryPolicyJson { get; set; }
    public int? TimeoutSeconds { get; set; }

    public ICollection<WorkflowExecution> Executions { get; set; } = [];
}
