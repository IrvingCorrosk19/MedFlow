using MedFlow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Analytics;

public sealed class AnalyticsSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AnalyticsSnapshotBackgroundService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    // Política de retry: hasta 3 reintentos con backoff exponencial (2m, 4m, 8m).
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMinutes(2);

    public AnalyticsSnapshotBackgroundService(
        IServiceProvider services,
        ILogger<AnalyticsSnapshotBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsSnapshotBackgroundService iniciado.");
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            await ProcessWithRetryAsync(yesterday, stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(DateTime date, CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = _services.CreateScope();
                await BeatAsync(scope.ServiceProvider, stoppingToken);
                var processor = scope.ServiceProvider.GetRequiredService<AnalyticsSnapshotProcessorService>();
                await processor.ProcessDateAsync(date, stoppingToken);
                await BeatAsync(scope.ServiceProvider, stoppingToken, "Completed");

                _logger.LogInformation(
                    "Analytics snapshot para {Date} completado (intento {Attempt}/{Max}).",
                    date.ToString("yyyy-MM-dd"), attempt, MaxRetries);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Analytics snapshot para {Date} falló en intento {Attempt}/{Max}.",
                    date.ToString("yyyy-MM-dd"), attempt, MaxRetries);

                using var errorScope = _services.CreateScope();
                await BeatAsync(errorScope.ServiceProvider, stoppingToken, "Failed", ex.Message);

                if (attempt < MaxRetries)
                {
                    // Backoff exponencial: 2m, 4m, 8m
                    var delay = TimeSpan.FromTicks(RetryBaseDelay.Ticks * (long)Math.Pow(2, attempt - 1));
                    _logger.LogWarning(
                        "Reintentando analytics snapshot en {DelayMinutes} minutos...",
                        (int)delay.TotalMinutes);
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    _logger.LogCritical(
                        "Analytics snapshot para {Date} falló después de {Max} intentos. " +
                        "Los datos de analytics estarán desactualizados hasta el próximo ciclo.",
                        date.ToString("yyyy-MM-dd"), MaxRetries);
                }
            }
        }
    }

    private static async Task BeatAsync(
        IServiceProvider sp,
        CancellationToken ct,
        string status = "Running",
        string? details = null)
    {
        try
        {
            var heartbeat = sp.GetRequiredService<IWorkerHeartbeatService>();
            await heartbeat.BeatAsync("AnalyticsSnapshotProcessor", status, details, ct);
        }
        catch { /* ignorar fallo del heartbeat para no afectar el proceso principal */ }
    }
}
