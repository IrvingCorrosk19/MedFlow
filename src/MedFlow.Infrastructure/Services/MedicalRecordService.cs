using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IApplicationDbContext _context;

    public MedicalRecordService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalRecord?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var q = _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Include(m => m.Appointment)
            .Include(m => m.Prescriptions)
            .Include(m => m.Attachments)
            .AsQueryable();

        if (includeDeleted)
            q = q.IgnoreQueryFilters();

        return await q.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetHistoryByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Doctor)
            .Include(m => m.Prescriptions)
            .Include(m => m.Attachments)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.VisitDate)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalRecord> CreateAsync(MedicalRecord record, IReadOnlyList<Prescription>? prescriptions, CancellationToken cancellationToken = default)
    {
        await _context.MedicalRecords.AddAsync(record, cancellationToken);
        if (prescriptions != null && prescriptions.Count > 0)
        {
            foreach (var p in prescriptions)
            {
                p.MedicalRecordId = record.Id;
                await _context.Prescriptions.AddAsync(p, cancellationToken);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<MedicalRecord> UpdateAsync(MedicalRecord record, IReadOnlyList<Prescription>? prescriptions, CancellationToken cancellationToken = default)
    {
        record.UpdatedAt = DateTime.UtcNow;
        _context.MedicalRecords.Update(record);

        var existing = await _context.Prescriptions
            .Where(p => p.MedicalRecordId == record.Id)
            .ToListAsync(cancellationToken);
        _context.Prescriptions.RemoveRange(existing);

        if (prescriptions != null && prescriptions.Count > 0)
        {
            foreach (var p in prescriptions)
            {
                p.Id = Guid.NewGuid();
                p.MedicalRecordId = record.Id;
                p.CreatedAt = DateTime.UtcNow;
                p.IsActive = true;
                p.IsDeleted = false;
                await _context.Prescriptions.AddAsync(p, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<MedicalRecord>> SearchAsync(
        string query,
        Guid? patientId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var q = _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Where(m => !m.IsDeleted);

        if (patientId.HasValue)
            q = q.Where(m => m.PatientId == patientId.Value);

        if (doctorId.HasValue)
            q = q.Where(m => m.DoctorId == doctorId.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim().ToLower();
            q = q.Where(m =>
                (m.ChiefComplaint != null && m.ChiefComplaint.ToLower().Contains(s)) ||
                (m.Diagnosis != null && m.Diagnosis.ToLower().Contains(s)) ||
                (m.TreatmentPlan != null && m.TreatmentPlan.ToLower().Contains(s)) ||
                (m.ClinicalNotes != null && m.ClinicalNotes.ToLower().Contains(s)));
        }

        return await q.OrderByDescending(m => m.VisitDate).Take(200).ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, false, cancellationToken);
        if (entity == null) return;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _context.MedicalRecords.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
