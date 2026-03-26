using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.AI;

public sealed class AIInsightProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AIInsightProcessorBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public AIInsightProcessorBackgroundService(IServiceProvider services, ILogger<AIInsightProcessorBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AIInsightProcessorBackgroundService started");
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AIInsightProcessor iteration failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IAIInsightProcessorService>();

        tenantContext.SetIgnoreTenantFilter(true);

        try
        {
            var tenantIds = await db.Tenants
                .Where(t => !t.IsDeleted && !t.IsSuspended)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    await processor.ProcessTenantAsync(tenantId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI processing failed for tenant {TenantId}", tenantId);
                }
            }

            if (tenantIds.Count > 0)
                _logger.LogInformation("AI processing completed for {Count} tenants", tenantIds.Count);
        }
        finally
        {
            tenantContext.SetIgnoreTenantFilter(false);
        }
    }
}
