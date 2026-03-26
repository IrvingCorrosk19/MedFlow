using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Workflow;

public sealed class WorkflowTriggerService : IWorkflowTriggerService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<WorkflowTriggerService> _logger;

    public WorkflowTriggerService(IApplicationDbContext context, ILogger<WorkflowTriggerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TriggerFromEventAsync(EventLog eventLog, CancellationToken cancellationToken = default)
    {
        var definitions = await _context.WorkflowDefinitions
            .AsNoTracking()
            .Where(w => w.TenantId == eventLog.TenantId
                && w.TriggerEvent == eventLog.EventType
                && w.IsActive
                && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        if (definitions.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var def in definitions)
        {
            var execPolicy = RetryPolicy.Parse(def.RetryPolicyJson);
            var execution = new WorkflowExecution
            {
                TenantId = eventLog.TenantId,
                WorkflowDefinitionId = def.Id,
                EventType = eventLog.EventType,
                AggregateId = string.IsNullOrEmpty(eventLog.AggregateId) ? null : eventLog.AggregateId,
                PayloadJson = eventLog.PayloadJson,
                Status = WorkflowExecutionStatus.Pending,
                AttemptCount = 0,
                MaxAttempts = execPolicy.MaxAttempts,
                NextAttemptAt = now,
                StartedAt = now
            };
            await _context.WorkflowExecutions.AddAsync(execution, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Triggered {Count} workflow(s) for event {EventType} tenant {TenantId}",
            definitions.Count, eventLog.EventType, eventLog.TenantId);
    }
}
