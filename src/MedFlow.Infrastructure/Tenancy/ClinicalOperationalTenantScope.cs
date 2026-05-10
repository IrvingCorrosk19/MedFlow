using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;

namespace MedFlow.Infrastructure.Tenancy;

/// <summary>
/// Cuando <see cref="ITenantContext.IgnoreTenantFilter"/> está activo (p. ej. SuperAdmin),
/// el filtro global de EF no restringe por tenant y los listados mezclarían todas las clínicas.
/// Los datos operativos deben acotarse al tenant resuelto en la petición (host, cabecera X-Tenant-Code, etc.).
/// </summary>
internal static class ClinicalOperationalTenantScope
{
    public static IQueryable<Patient> ApplyToPatients(ITenantContext tenant, IQueryable<Patient> query)
    {
        if (tenant.IgnoreTenantFilter && tenant.TenantId.HasValue)
            return query.Where(p => p.TenantId == tenant.TenantId.Value);
        return query;
    }

    public static IQueryable<Doctor> ApplyToDoctors(ITenantContext tenant, IQueryable<Doctor> query)
    {
        if (tenant.IgnoreTenantFilter && tenant.TenantId.HasValue)
            return query.Where(d => d.TenantId == tenant.TenantId.Value);
        return query;
    }

    public static IQueryable<Appointment> ApplyToAppointments(ITenantContext tenant, IQueryable<Appointment> query)
    {
        if (tenant.IgnoreTenantFilter && tenant.TenantId.HasValue)
            return query.Where(a => a.TenantId == tenant.TenantId.Value);
        return query;
    }
}
