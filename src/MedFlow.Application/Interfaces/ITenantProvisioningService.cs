using MedFlow.Application.Onboarding;

namespace MedFlow.Application.Interfaces;

public interface ITenantProvisioningService
{
    /// <summary>Crea tenant, suscripción, usuario administrador, settings, eventos y auditoría en una transacción.</summary>
    Task<TenantProvisioningResult> ProvisionAsync(
        TenantProvisioningRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);
}
