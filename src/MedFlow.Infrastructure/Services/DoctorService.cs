using MedFlow.Application.Common;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionLimitService _limits;

    public DoctorService(IApplicationDbContext context, ITenantContext tenant, ISubscriptionLimitService limits)
    {
        _context = context;
        _tenant = tenant;
        _limits = limits;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Doctor>> GetAllAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.Doctors.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d =>
                (d.FirstName + " " + d.LastName).ToLower().Contains(s) ||
                (d.Speciality != null && d.Speciality.ToLower().Contains(s)) ||
                (d.Email != null && d.Email.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Doctor>> GetPagedAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.Doctors.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d =>
                (d.FirstName + " " + d.LastName).ToLower().Contains(s) ||
                (d.Speciality != null && d.Speciality.ToLower().Contains(s)) ||
                (d.Email != null && d.Email.ToLower().Contains(s)));
        }

        var ordered = query.OrderBy(d => d.LastName).ThenBy(d => d.FirstName);
        var total = await query.CountAsync(cancellationToken);
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Doctor>(items, total, page, pageSize);
    }

    public async Task<(Doctor? Doctor, string? Error)> CreateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        var tid = doctor.TenantId != Guid.Empty ? doctor.TenantId : _tenant.TenantId;
        if (!tid.HasValue)
            return (null, "No se pudo determinar la clínica para validar límites del plan.");

        var chk = await _limits.CanCreateDoctorAsync(tid.Value, cancellationToken);
        if (!chk.Allowed)
        {
            var msg = chk.Suggestion != null ? $"{chk.Message} {chk.Suggestion}" : chk.Message;
            return (null, msg);
        }

        await _context.Doctors.AddAsync(doctor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (doctor, null);
    }

    public async Task<Doctor> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        doctor.UpdatedAt = DateTime.UtcNow;
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync(cancellationToken);
        return doctor;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await GetByIdAsync(id, cancellationToken);
        if (doctor == null) return;

        doctor.IsDeleted = true;
        doctor.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
