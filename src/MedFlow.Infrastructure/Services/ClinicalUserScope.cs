using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public sealed class ClinicalUserScope : IClinicalUserScope
{
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public ClinicalUserScope(
        IHttpContextAccessor http,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext db,
        ITenantContext tenant)
    {
        _http = http;
        _userManager = userManager;
        _db = db;
        _tenant = tenant;
    }

    public async Task<(bool RestrictToDoctor, Guid? LinkedDoctorId)> GetDoctorDataScopeAsync(CancellationToken cancellationToken = default)
    {
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return (false, null);

        var user = await _userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user == null)
            return (false, null);

        if (await _userManager.IsInRoleAsync(user, "SuperAdmin").ConfigureAwait(false))
            return (false, null);
        if (await _userManager.IsInRoleAsync(user, "Admin").ConfigureAwait(false))
            return (false, null);
        if (await _userManager.IsInRoleAsync(user, "Reception").ConfigureAwait(false))
            return (false, null);
        if (await _userManager.IsInRoleAsync(user, "Billing").ConfigureAwait(false))
            return (false, null);
        if (await _userManager.IsInRoleAsync(user, "Staff").ConfigureAwait(false))
            return (false, null);

        if (!await _userManager.IsInRoleAsync(user, "Doctor").ConfigureAwait(false))
            return (false, null);

        var tid = _tenant.TenantId;
        if (!tid.HasValue)
            return (true, null);

        var doctor = await _db.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.UserId == user.Id && d.TenantId == tid.Value && !d.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        return doctor == null ? (true, null) : (true, doctor.Id);
    }

    public async Task<bool> DoctorHasClinicalRelationshipWithPatientAsync(
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var hasApt = await _db.Appointments.AsNoTracking()
            .AnyAsync(a => !a.IsDeleted && a.DoctorId == doctorId && a.PatientId == patientId, cancellationToken)
            .ConfigureAwait(false);
        if (hasApt)
            return true;

        return await _db.MedicalRecords.AsNoTracking()
            .AnyAsync(m => !m.IsDeleted && m.DoctorId == doctorId && m.PatientId == patientId, cancellationToken)
            .ConfigureAwait(false);
    }
}
