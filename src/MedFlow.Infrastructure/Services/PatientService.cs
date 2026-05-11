using MedFlow.Application.Common;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly IClinicalUserScope _clinicalScope;
    private readonly ISubscriptionLimitService _limits;
    private readonly IEventLogService _eventLog;
    private readonly IAuditLogService _audit;

    public PatientService(
        IApplicationDbContext context,
        ITenantContext tenant,
        IClinicalUserScope clinicalScope,
        ISubscriptionLimitService limits,
        IEventLogService eventLog,
        IAuditLogService audit)
    {
        _context = context;
        _tenant = tenant;
        _clinicalScope = clinicalScope;
        _limits = limits;
        _eventLog = eventLog;
        _audit = audit;
    }

    private async Task<IQueryable<Patient>> ApplyDoctorDirectoryAsync(IQueryable<Patient> query, CancellationToken cancellationToken)
    {
        var (restrict, docId) = await _clinicalScope.GetDoctorDataScopeAsync(cancellationToken).ConfigureAwait(false);
        if (!restrict)
            return query;
        if (!docId.HasValue)
            return query.Where(p => false);
        return ClinicalDoctorPatientFilter.Apply(_context, docId.Value, query);
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = ClinicalOperationalTenantScope.ApplyToPatients(_tenant, _context.Patients.AsNoTracking());
        query = await ApplyDoctorDirectoryAsync(query, cancellationToken).ConfigureAwait(false);
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 100,
        string? documento = null,
        string? telefono = null,
        int? edadDesde = null,
        int? edadHasta = null,
        CancellationToken cancellationToken = default)
    {
        // Límites seguros: mínimo 1, máximo 500 por página para evitar OOM.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = ClinicalOperationalTenantScope.ApplyToPatients(_tenant, _context.Patients.AsNoTracking().AsQueryable());
        query = await ApplyDoctorDirectoryAsync(query, cancellationToken).ConfigureAwait(false);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                (p.PrimerNombre + " " + (p.SegundoNombre ?? "") + " " + p.PrimerApellido + " " + (p.SegundoApellido ?? "")).ToLower().Contains(s) ||
                (p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(s)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(s)) ||
                (p.Telefono != null && p.Telefono.Contains(search)) ||
                (p.Observaciones != null && p.Observaciones.ToLower().Contains(s)) ||
                (p.Alergias != null && p.Alergias.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var d = documento.Trim().ToLower();
            query = query.Where(p => p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(telefono))
        {
            var t = telefono.Trim();
            query = query.Where(p => p.Telefono != null && p.Telefono.Contains(t));
        }

        if (edadDesde.HasValue || edadHasta.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (edadHasta.HasValue)
            {
                // age >= edadHasta means born on or before today - edadHasta years
                var maxBirth = today.AddYears(-edadHasta.Value);
                query = query.Where(p => p.FechaNacimiento.HasValue &&
                    DateOnly.FromDateTime(p.FechaNacimiento.Value) <= maxBirth);
            }
            if (edadDesde.HasValue)
            {
                // age <= edadDesde means born on or after today - edadDesde years
                var minBirth = today.AddYears(-edadDesde.Value);
                query = query.Where(p => p.FechaNacimiento.HasValue &&
                    DateOnly.FromDateTime(p.FechaNacimiento.Value) >= minBirth);
            }
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.PrimerApellido)
            .ThenBy(p => p.SegundoApellido)
            .ThenBy(p => p.PrimerNombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 25,
        string? documento = null,
        string? telefono = null,
        int? edadDesde = null,
        int? edadHasta = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = ClinicalOperationalTenantScope.ApplyToPatients(_tenant, _context.Patients.AsNoTracking().AsQueryable());
        query = await ApplyDoctorDirectoryAsync(query, cancellationToken).ConfigureAwait(false);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                (p.PrimerNombre + " " + (p.SegundoNombre ?? "") + " " + p.PrimerApellido + " " + (p.SegundoApellido ?? "")).ToLower().Contains(s) ||
                (p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(s)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(s)) ||
                (p.Telefono != null && p.Telefono.Contains(search)) ||
                (p.Observaciones != null && p.Observaciones.ToLower().Contains(s)) ||
                (p.Alergias != null && p.Alergias.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var d = documento.Trim().ToLower();
            query = query.Where(p => p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(d));
        }

        if (!string.IsNullOrWhiteSpace(telefono))
        {
            var t = telefono.Trim();
            query = query.Where(p => p.Telefono != null && p.Telefono.Contains(t));
        }

        if (edadDesde.HasValue || edadHasta.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (edadHasta.HasValue)
            {
                var maxBirth = today.AddYears(-edadHasta.Value);
                query = query.Where(p => p.FechaNacimiento.HasValue &&
                    DateOnly.FromDateTime(p.FechaNacimiento.Value) <= maxBirth);
            }
            if (edadDesde.HasValue)
            {
                var minBirth = today.AddYears(-edadDesde.Value);
                query = query.Where(p => p.FechaNacimiento.HasValue &&
                    DateOnly.FromDateTime(p.FechaNacimiento.Value) >= minBirth);
            }
        }

        var ordered = query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.PrimerApellido)
            .ThenBy(p => p.SegundoApellido)
            .ThenBy(p => p.PrimerNombre);
        var total = await query.CountAsync(cancellationToken);
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Patient>(items, total, page, pageSize);
    }

    public async Task<(Patient? Patient, string? Error)> CreateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        var tid = patient.TenantId != Guid.Empty ? patient.TenantId : _tenant.TenantId;
        if (!tid.HasValue)
            return (null, "No se pudo determinar la clínica para validar límites del plan.");

        var chk = await _limits.CanCreatePatientAsync(tid.Value, cancellationToken);
        if (!chk.Allowed)
        {
            var msg = chk.Suggestion != null ? $"{chk.Message} {chk.Suggestion}" : chk.Message;
            return (null, msg);
        }

        if (!string.IsNullOrWhiteSpace(patient.NumeroDocumento))
        {
            var nd = patient.NumeroDocumento.Trim().ToLower();
            var dup = await _context.Patients.AsNoTracking().AnyAsync(p =>
                !p.IsDeleted && p.TenantId == tid.Value && p.NumeroDocumento != null &&
                p.NumeroDocumento.Trim().ToLower() == nd, cancellationToken);
            if (dup)
                return (null, "Ya existe un paciente con ese número de documento en esta clínica.");
        }

        await _context.Patients.AddAsync(patient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventLog.EnqueueAsync("PatientCreated", new
        {
            patient.Id
        }, "Patient", patient.Id.ToString(), cancellationToken);

        await _audit.LogAsync(new AuditLogWriteDto("Create", "Patients", nameof(Patient), patient.Id.ToString(),
            $"Paciente {patient.NombreCompleto}"), cancellationToken);

        return (patient, null);
    }

    public async Task<Patient> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(patient.NumeroDocumento))
        {
            var nd = patient.NumeroDocumento.Trim().ToLower();
            var dup = await _context.Patients.AsNoTracking().AnyAsync(p =>
                !p.IsDeleted && p.TenantId == patient.TenantId && p.Id != patient.Id &&
                p.NumeroDocumento != null && p.NumeroDocumento.Trim().ToLower() == nd, cancellationToken);
            if (dup)
                throw new InvalidOperationException("Ya existe otro paciente con ese número de documento en esta clínica.");
        }

        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Patients", nameof(Patient), patient.Id.ToString(),
            $"Paciente {patient.NombreCompleto}"), cancellationToken);
        return patient;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var patient = await GetByIdAsync(id, cancellationToken);
        if (patient == null)
            return false;

        patient.IsActive = isActive;
        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Patients", nameof(Patient), id.ToString(),
            isActive ? "Paciente reactivado" : "Paciente desactivado"), cancellationToken);
        return true;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await GetByIdAsync(id, cancellationToken);
        if (patient == null) return;

        var todayUtc = DateTime.UtcNow.Date;
        var futureApt = await _context.Appointments.AsNoTracking()
            .AnyAsync(a => !a.IsDeleted && a.PatientId == id &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.NoShow &&
                a.ScheduledDate >= todayUtc, cancellationToken);
        if (futureApt)
            throw new InvalidOperationException("No se puede eliminar: el paciente tiene citas futuras no canceladas.");

        var pendingInv = await _context.BillingInvoices.AsNoTracking()
            .AnyAsync(i => !i.IsDeleted && i.PatientId == id && i.BalanceDue > 0.01m, cancellationToken);
        if (pendingInv)
            throw new InvalidOperationException("No se puede eliminar: hay facturas con saldo pendiente.");

        patient.IsDeleted = true;
        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Delete", "Patients", nameof(Patient), id.ToString(),
            "Paciente eliminado (soft)"), cancellationToken);
    }
}
