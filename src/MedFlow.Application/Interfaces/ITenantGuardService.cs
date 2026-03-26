namespace MedFlow.Application.Interfaces;

public interface ITenantGuardService
{
    void AssertSameTenant(Guid expectedTenantId, Guid? actualTenantId);
    void AssertTenantAccess(Guid tenantId);
    bool ValidateEntityBelongsToTenant(Guid entityTenantId, Guid? contextTenantId);
}
