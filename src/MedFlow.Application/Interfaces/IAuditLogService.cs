using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public record AuditLogWriteDto(
    string Action,
    string Module,
    string? EntityName,
    string? EntityId,
    string? Description,
    string? OldValuesJson = null,
    string? NewValuesJson = null);

public interface IAuditLogService
{
    Task LogAsync(AuditLogWriteDto dto, CancellationToken cancellationToken = default);
    Task LogAsync(AuditLogWriteDto dto, string? userId, string? userName, string? ipAddress, CancellationToken cancellationToken = default);
    /// <summary>Registra auditoría para un tenant específico (ej. procesamiento en background).</summary>
    Task LogForTenantAsync(Guid tenantId, AuditLogWriteDto dto, string? userId = null, string? userName = null, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(DateTime? fromUtc, DateTime? toUtc, string? userId, string? module, string? action, int take, CancellationToken cancellationToken = default);
}
