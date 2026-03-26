namespace MedFlow.Domain.Common;

/// <summary>Entidad cuyo aislamiento multi-tenant se aplica vía TenantId y filtros globales EF.</summary>
public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
