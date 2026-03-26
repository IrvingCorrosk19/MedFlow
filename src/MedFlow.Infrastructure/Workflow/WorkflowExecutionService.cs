using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Workflow;

public sealed class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;

    public WorkflowExecutionService(
        IApplicationDbContext context,
        ITenantContext tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowExecutions
            .Include(e => e.WorkflowDefinition)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecution>> ListAsync(WorkflowExecutionListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowExecutions
            .Include(e => e.WorkflowDefinition)
            .AsQueryable();

        if (_tenant.TenantId.HasValue)
            query = query.Where(e => e.TenantId == _tenant.TenantId.Value);

        if (filter.WorkflowDefinitionId.HasValue)
            query = query.Where(e => e.WorkflowDefinitionId == filter.WorkflowDefinitionId.Value);
        if (filter.Status.HasValue)
            query = query.Where(e => e.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.EventType))
            query = query.Where(e => e.EventType == filter.EventType);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowMetrics> GetMetricsAsync(WorkflowMetricsFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new WorkflowMetricsFilter();
        var query = _context.WorkflowExecutions.AsQueryable();

        if (_tenant.TenantId.HasValue)
            query = query.Where(e => e.TenantId == _tenant.TenantId.Value);
        if (filter.WorkflowDefinitionId.HasValue)
            query = query.Where(e => e.WorkflowDefinitionId == filter.WorkflowDefinitionId.Value);
        if (filter.From.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.To.Value);

        var all = await query.ToListAsync(cancellationToken);
        var completed = all.Where(e => e.CompletedAt.HasValue && e.StartedAt != default).ToList();
        var avgSeconds = completed.Count > 0
            ? completed.Average(e => (e.CompletedAt!.Value - e.StartedAt).TotalSeconds)
            : 0;

        var topErrors = all
            .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
            .GroupBy(e => e.ErrorMessage!.Length > 200 ? e.ErrorMessage[..200] : e.ErrorMessage!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new WorkflowErrorSummary(g.Key, g.Count()))
            .ToList();

        return new WorkflowMetrics(
            all.Count,
            all.Count(e => e.Status == WorkflowExecutionStatus.Succeeded),
            all.Count(e => e.Status == WorkflowExecutionStatus.Failed),
            all.Count(e => e.Status == WorkflowExecutionStatus.Pending),
            all.Count(e => e.Status == WorkflowExecutionStatus.Retrying),
            avgSeconds,
            topErrors);
    }

    public async Task RetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exec = await _context.WorkflowExecutions
            .Include(e => e.WorkflowDefinition)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {id} not found");

        if (exec.Status != WorkflowExecutionStatus.Failed)
            throw new InvalidOperationException("Only failed executions can be retried");

        exec.Status = WorkflowExecutionStatus.Pending;
        exec.NextAttemptAt = DateTime.UtcNow;
        exec.ErrorMessage = null;
        exec.ResponseStatusCode = null;
        exec.ResponseBody = null;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
