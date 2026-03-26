using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Workflow;

public sealed class WorkflowProcessorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WorkflowProcessorService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    public WorkflowProcessorService(IServiceProvider services, ILogger<WorkflowProcessorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowProcessorService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEventLogsAsync(stoppingToken);
                await ProcessWorkflowExecutionsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WorkflowProcessorService iteration failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessEventLogsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var triggerService = scope.ServiceProvider.GetRequiredService<IWorkflowTriggerService>();

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetIgnoreTenantFilter(true);

        try
        {
            var pendingEvents = await context.EventLogs
                .Where(e => e.Status == OutboxEventStatus.Pending && !e.IsDeleted)
                .OrderBy(e => e.CreatedAt)
                .Take(50)
                .ToListAsync(ct);

            foreach (var evt in pendingEvents)
            {
                try
                {
                    await triggerService.TriggerFromEventAsync(evt, ct);
                    evt.Status = OutboxEventStatus.Processed;
                    evt.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    evt.Status = OutboxEventStatus.Failed;
                    evt.LastError = ex.Message.Length > 3990 ? ex.Message[..3990] : ex.Message;
                    _logger.LogError(ex, "Failed to trigger workflows for event {EventId}", evt.Id);
                }
            }

            if (pendingEvents.Count > 0)
                await context.SaveChangesAsync(ct);
        }
        finally
        {
            tenantContext.SetIgnoreTenantFilter(false);
        }
    }

    private async Task ProcessWorkflowExecutionsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IWorkflowDispatcher>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetIgnoreTenantFilter(true);

        try
        {
            var now = DateTime.UtcNow;
            var pending = await context.WorkflowExecutions
                .Include(e => e.WorkflowDefinition)
                .Where(e => (e.Status == WorkflowExecutionStatus.Pending || e.Status == WorkflowExecutionStatus.Retrying)
                    && e.NextAttemptAt <= now
                    && !e.IsDeleted
                    && e.WorkflowDefinition != null
                    && !e.WorkflowDefinition.IsDeleted)
                .OrderBy(e => e.NextAttemptAt)
                .Take(25)
                .ToListAsync(ct);

            foreach (var exec in pending)
            {
                exec.Status = WorkflowExecutionStatus.Processing;
                exec.AttemptCount++;
                exec.LastAttemptAt = now;
                await context.SaveChangesAsync(ct);

                var result = await dispatcher.DispatchAsync(exec, exec.WorkflowDefinition, ct);

                exec.ResponseStatusCode = result.StatusCode;
                exec.ResponseBody = result.ResponseBody != null && result.ResponseBody.Length > 7990
                    ? result.ResponseBody[..7990] + "..."
                    : result.ResponseBody;
                exec.ErrorMessage = result.ErrorMessage;

                if (result.Success)
                {
                    exec.Status = WorkflowExecutionStatus.Succeeded;
                    exec.CompletedAt = now;
                    exec.NextAttemptAt = null;
                }
                else
                {
                    var policy = RetryPolicy.Parse(exec.WorkflowDefinition.RetryPolicyJson);
                    if (exec.AttemptCount >= exec.MaxAttempts)
                    {
                        exec.Status = WorkflowExecutionStatus.Failed;
                        exec.CompletedAt = now;
                        exec.NextAttemptAt = null;
                        _logger.LogWarning("Workflow execution {ExecutionId} failed after {Attempts} attempts",
                            exec.Id, exec.AttemptCount);
                    }
                    else
                    {
                        exec.Status = WorkflowExecutionStatus.Retrying;
                        var delay = policy.GetDelaySeconds(exec.AttemptCount);
                        exec.NextAttemptAt = now.AddSeconds(delay);
                        _logger.LogInformation("Workflow execution {ExecutionId} will retry in {Seconds}s (attempt {Attempt})",
                            exec.Id, delay, exec.AttemptCount);
                    }
                }

                await context.SaveChangesAsync(ct);
            }
        }
        finally
        {
            tenantContext.SetIgnoreTenantFilter(false);
        }
    }
}
