using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Workflow;

public sealed class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogService _audit;

    public WorkflowDefinitionService(IApplicationDbContext context, ITenantContext tenant, IAuditLogService audit)
    {
        _context = context;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListByTenantAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.TenantId.HasValue)
            return [];

        return await _context.WorkflowDefinitions
            .Where(w => w.TenantId == _tenant.TenantId.Value)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowDefinition> CreateAsync(CreateWorkflowDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        var (valid, error) = WebhookUrlValidator.Validate(command.WebhookUrl, requireHttpsInProduction: isProduction);
        if (!valid)
            throw new ArgumentException(error ?? "URL de webhook inválida");

        var entity = new WorkflowDefinition
        {
            TenantId = _tenant.TenantId.Value,
            Name = command.Name,
            Code = command.Code.Trim().ToLowerInvariant().Replace(" ", "-"),
            Description = command.Description,
            TriggerEvent = command.TriggerEvent,
            IsActive = true,
            WebhookUrl = command.WebhookUrl,
            HttpMethod = command.HttpMethod ?? "POST",
            HeadersJson = command.HeadersJson,
            PayloadTemplateJson = command.PayloadTemplateJson ?? "{}",
            RetryPolicyJson = command.RetryPolicyJson,
            TimeoutSeconds = command.TimeoutSeconds
        };
        await _context.WorkflowDefinitions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Create", "Workflow", "WorkflowDefinition", entity.Id.ToString(), $"Workflow '{entity.Name}' creado", null, global::System.Text.Json.JsonSerializer.Serialize(new { entity.Code, entity.TriggerEvent })), cancellationToken);
        return entity;
    }

    public async Task<WorkflowDefinition> UpdateAsync(UpdateWorkflowDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {command.Id} not found");

        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        var (valid, error) = WebhookUrlValidator.Validate(command.WebhookUrl, requireHttpsInProduction: isProduction);
        if (!valid)
            throw new ArgumentException(error ?? "URL de webhook inválida");

        entity.Name = command.Name;
        entity.Code = command.Code.Trim().ToLowerInvariant().Replace(" ", "-");
        entity.Description = command.Description;
        entity.TriggerEvent = command.TriggerEvent;
        entity.IsActive = command.IsActive;
        entity.WebhookUrl = command.WebhookUrl;
        entity.HttpMethod = command.HttpMethod ?? "POST";
        entity.HeadersJson = command.HeadersJson;
        entity.PayloadTemplateJson = command.PayloadTemplateJson ?? "{}";
        entity.RetryPolicyJson = command.RetryPolicyJson;
        entity.TimeoutSeconds = command.TimeoutSeconds;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Workflow", "WorkflowDefinition", entity.Id.ToString(), $"Workflow '{entity.Name}' actualizado", null, global::System.Text.Json.JsonSerializer.Serialize(new { command.IsActive })), cancellationToken);
        return entity;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {id} not found");
        entity.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto(isActive ? "Activate" : "Deactivate", "Workflow", "WorkflowDefinition", id.ToString(), $"Workflow {(isActive ? "activado" : "desactivado")}", null, null), cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {id} not found");
        var name = entity.Name;
        entity.IsDeleted = true;
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Delete", "Workflow", "WorkflowDefinition", id.ToString(), $"Workflow '{name}' eliminado", null, null), cancellationToken);
    }
}
