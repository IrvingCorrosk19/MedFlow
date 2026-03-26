using MedFlow.Application.Exceptions;
using MedFlow.Application.Interfaces;

namespace MedFlow.Infrastructure.Services;

public sealed class TenantGuardService : ITenantGuardService
{
    private readonly ITenantContext _tenant;

    public TenantGuardService(ITenantContext tenant)
    {
        _tenant = tenant;
    }

    public void AssertSameTenant(Guid expectedTenantId, Guid? actualTenantId)
    {
        if (!actualTenantId.HasValue)
            throw new TenantResolutionException("Tenant context is required for this operation.");
        if (actualTenantId.Value != expectedTenantId)
            throw new TenantResolutionException("Access denied. Entity belongs to a different tenant.");
    }

    public void AssertTenantAccess(Guid tenantId)
    {
        if (!_tenant.TenantId.HasValue)
            throw new TenantResolutionException("Tenant resolution required.");
        if (_tenant.TenantId.Value != tenantId)
            throw new TenantResolutionException("Access denied. Tenant mismatch.");
    }

    public bool ValidateEntityBelongsToTenant(Guid entityTenantId, Guid? contextTenantId)
    {
        if (!contextTenantId.HasValue) return false;
        return entityTenantId == contextTenantId.Value;
    }
}
