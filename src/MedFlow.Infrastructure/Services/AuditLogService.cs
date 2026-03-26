using System.Security.Claims;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IApplicationDbContext context, ITenantContext tenant, IHttpContextAccessor http, ILogger<AuditLogService> logger)
    {
        _context = context;
        _tenant = tenant;
        _http = http;
        _logger = logger;
    }

    public Task LogAsync(AuditLogWriteDto dto, CancellationToken cancellationToken = default)
    {
        var user = _http.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user?.Identity?.Name;
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        return LogAsync(dto, userId, userName, ip, cancellationToken);
    }

    public async Task LogForTenantAsync(Guid tenantId, AuditLogWriteDto dto, string? userId = null, string? userName = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                UserName = userName,
                Action = dto.Action,
                Module = dto.Module,
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                Description = dto.Description,
                OldValuesJson = dto.OldValuesJson,
                NewValuesJson = dto.NewValuesJson,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuditLog write failed for tenant {TenantId} action {Action}", tenantId, dto.Action);
        }
    }

    public async Task LogAsync(AuditLogWriteDto dto, string? userId, string? userName, string? ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_tenant.TenantId.HasValue)
                return;

            var log = new AuditLog
            {
                TenantId = _tenant.TenantId.Value,
                UserId = userId,
                UserName = userName,
                Action = dto.Action,
                Module = dto.Module,
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                Description = dto.Description,
                OldValuesJson = dto.OldValuesJson,
                NewValuesJson = dto.NewValuesJson,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuditLog write failed for action {Action} entity {EntityName}/{EntityId}", dto.Action, dto.EntityName, dto.EntityId);
        }
    }

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(DateTime? fromUtc, DateTime? toUtc, string? userId, string? module, string? action, int take, CancellationToken cancellationToken = default)
    {
        var q = _context.AuditLogs.AsNoTracking().AsQueryable();
        if (_tenant.TenantId.HasValue)
            q = q.Where(a => a.TenantId == _tenant.TenantId.Value);
        if (fromUtc.HasValue)
            q = q.Where(a => a.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            q = q.Where(a => a.CreatedAt <= toUtc.Value);
        if (!string.IsNullOrWhiteSpace(userId))
            q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(module))
            q = q.Where(a => a.Module == module);
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action.Contains(action));

        return await q
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(take, 1, 2000))
            .ToListAsync(cancellationToken);
    }
}
