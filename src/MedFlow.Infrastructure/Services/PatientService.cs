using MedFlow.Application.Common;
using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionLimitService _limits;
    private readonly IEventLogService _eventLog;
    private readonly IAuditLogService _audit;

    public PatientService(
        IApplicationDbContext context,
        ITenantContext tenant,
        ISubscriptionLimitService limits,
        IEventLogService eventLog,
        IAuditLogService audit)
    {
        _context = context;
        _tenant = tenant;
        _limits = limits;
        _eventLog = eventLog;
        _audit = audit;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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

        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                (p.PrimerNombre + " " + (p.SegundoNombre ?? "") + " " + p.PrimerApellido + " " + (p.SegundoApellido ?? "")).ToLower().Contains(s) ||
                (p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(s)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(s)) ||
                (p.Telefono != null && p.Telefono.Contains(search)));
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
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                (p.PrimerNombre + " " + (p.SegundoNombre ?? "") + " " + p.PrimerApellido + " " + (p.SegundoApellido ?? "")).ToLower().Contains(s) ||
                (p.NumeroDocumento != null && p.NumeroDocumento.ToLower().Contains(s)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(s)) ||
                (p.Telefono != null && p.Telefono.Contains(search)));
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
        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Update", "Patients", nameof(Patient), patient.Id.ToString(),
            $"Paciente {patient.NombreCompleto}"), cancellationToken);
        return patient;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await GetByIdAsync(id, cancellationToken);
        if (patient == null) return;

        patient.IsDeleted = true;
        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogWriteDto("Delete", "Patients", nameof(Patient), id.ToString(),
            "Paciente eliminado (soft)"), cancellationToken);
    }
}
