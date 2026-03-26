using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Services;

public class EventLogService : IEventLogService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly ILogger<EventLogService> _logger;

    public EventLogService(IApplicationDbContext context, ITenantContext tenant, ILogger<EventLogService> logger)
    {
        _context = context;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task EnqueueAsync(string eventType, object payload, string? aggregateType = null, string? aggregateId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            if (!_tenant.TenantId.HasValue)
                return;

            var eventLog = new EventLog
            {
                TenantId = _tenant.TenantId.Value,
                EventType = eventType,
                PayloadJson = payloadJson,
                AggregateType = aggregateType ?? string.Empty,
                AggregateId = aggregateId ?? string.Empty,
                Status = OutboxEventStatus.Pending,
                ProcessedAt = null,
                RetryCount = 0,
                LastError = null
            };
            await _context.EventLogs.AddAsync(eventLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EventLog outbox write failed for event {EventType} aggregate {AggregateType}/{AggregateId}", eventType, aggregateType, aggregateId);
        }
    }

    public async Task EnqueueForTenantAsync(Guid tenantId, string eventType, object payload, string? aggregateType = null, string? aggregateId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var eventLog = new EventLog
            {
                TenantId = tenantId,
                EventType = eventType,
                PayloadJson = payloadJson,
                AggregateType = aggregateType ?? string.Empty,
                AggregateId = aggregateId ?? string.Empty,
                Status = OutboxEventStatus.Pending,
                ProcessedAt = null,
                RetryCount = 0,
                LastError = null
            };
            await _context.EventLogs.AddAsync(eventLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EventLog outbox write failed for event {EventType} tenant {TenantId}", eventType, tenantId);
        }
    }
}
