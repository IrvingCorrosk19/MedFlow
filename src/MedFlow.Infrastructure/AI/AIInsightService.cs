using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.AI;
using MedFlow.Domain;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.AI;

public sealed class AIInsightService : IAIInsightService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _audit;
    private readonly IEventLogService _eventLog;

    public AIInsightService(IApplicationDbContext context, IAuditLogService audit, IEventLogService eventLog)
    {
        _context = context;
        _audit = audit;
        _eventLog = eventLog;
    }

    public async Task<AIInsight> CreateAsync(CreateAIInsightCommand command, CancellationToken cancellationToken = default)
    {
        var insight = new AIInsight
        {
            TenantId = command.TenantId,
            InsightType = command.InsightType,
            EntityType = command.EntityType,
            EntityId = command.EntityId,
            Title = command.Title,
            Summary = command.Summary,
            Severity = command.Severity,
            Score = command.Score,
            Confidence = command.Confidence,
            Recommendation = command.Recommendation,
            EvidenceJson = command.EvidenceJson,
            Source = command.Source,
            GeneratedAt = DateTime.UtcNow,
            Status = AIInsightStatus.New
        };
        await _context.AIInsights.AddAsync(insight, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return insight;
    }

    public async Task<AIInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AIInsights.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AIInsight>> ListAsync(AIInsightFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AIInsights.AsQueryable();

        if (filter.TenantId.HasValue)
            query = query.Where(i => i.TenantId == filter.TenantId.Value);
        if (filter.InsightType.HasValue)
            query = query.Where(i => i.InsightType == filter.InsightType.Value);
        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status.Value);
        if (filter.Severity.HasValue)
            query = query.Where(i => i.Severity == filter.Severity.Value);
        if (filter.From.HasValue)
            query = query.Where(i => i.GeneratedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(i => i.GeneratedAt <= filter.To.Value);
        if (filter.MinScore.HasValue)
            query = query.Where(i => i.Score >= filter.MinScore.Value);
        if (filter.MinConfidence.HasValue)
            query = query.Where(i => i.Confidence >= filter.MinConfidence.Value);
        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(i => i.EntityType == filter.EntityType);
        if (!string.IsNullOrEmpty(filter.EntityId))
            query = query.Where(i => i.EntityId == filter.EntityId);

        return await query
            .OrderByDescending(i => i.GeneratedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<AIInsightDashboardMetrics> GetDashboardMetricsAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AIInsights.Where(i => i.TenantId == tenantId);
        if (from.HasValue)
            query = query.Where(i => i.GeneratedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(i => i.GeneratedAt <= to.Value);

        var all = await query.ToListAsync(cancellationToken);

        var byType = all
            .GroupBy(i => i.InsightType)
            .Select(g => new AIInsightTypeCount(g.Key, g.Key.ToString(), g.Count()))
            .ToList();

        var recentCritical = await _context.AIInsights
            .Where(i => i.TenantId == tenantId && i.Severity == AISeverity.Critical && i.Status == AIInsightStatus.New)
            .OrderByDescending(i => i.GeneratedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new AIInsightDashboardMetrics(
            all.Count,
            all.Count(i => i.Severity == AISeverity.Critical),
            all.Count(i => i.Severity == AISeverity.Warning),
            all.Count(i => i.Status == AIInsightStatus.New),
            all.Count(i => i.Status == AIInsightStatus.Acknowledged),
            byType,
            recentCritical);
    }

    public async Task AcknowledgeAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var insight = await _context.AIInsights.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Insight {id} not found");
        insight.Status = AIInsightStatus.Acknowledged;
        insight.AcknowledgedAt = DateTime.UtcNow;
        insight.AcknowledgedByUserId = userId;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Acknowledge", "AI", "AIInsight", id.ToString(), $"Insight {insight.InsightType} reconocido por usuario"), cancellationToken);
        await _eventLog.EnqueueForTenantAsync(insight.TenantId, WorkflowTriggerEvents.AIAlertAcknowledged, new { insight.Id, insight.InsightType, Action = "Acknowledged" }, "AIInsight", id.ToString(), cancellationToken);
    }

    public async Task DismissAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var insight = await _context.AIInsights.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Insight {id} not found");
        insight.Status = AIInsightStatus.Dismissed;
        insight.AcknowledgedAt = DateTime.UtcNow;
        insight.AcknowledgedByUserId = userId;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Dismiss", "AI", "AIInsight", id.ToString(), $"Insight {insight.InsightType} descartado por usuario"), cancellationToken);
        await _eventLog.EnqueueForTenantAsync(insight.TenantId, WorkflowTriggerEvents.AIAlertAcknowledged, new { insight.Id, insight.InsightType, Action = "Dismissed" }, "AIInsight", id.ToString(), cancellationToken);
    }
}
