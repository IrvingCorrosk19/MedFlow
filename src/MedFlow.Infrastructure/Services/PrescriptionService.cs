using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly IApplicationDbContext _db;

    public PrescriptionService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Prescription>> GetByMedicalRecordAsync(Guid medicalRecordId, CancellationToken ct = default)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Patient)
            .Where(p => p.MedicalRecordId == medicalRecordId && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Prescription>> GetByPatientAsync(Guid patientId, Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Patient)
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Doctor)
            .Where(p => p.TenantId == tenantId
                        && !p.IsDeleted
                        && p.MedicalRecord.PatientId == patientId)
            .OrderByDescending(p => p.IssuedAt)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Prescription?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Patient)
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted, ct);
    }

    public async Task VoidAsync(Guid id, Guid tenantId, string reason, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted, ct);

        if (prescription == null) return;

        prescription.IsVoid = true;
        prescription.VoidReason = reason;
        prescription.UpdatedAt = DateTime.UtcNow;
        _db.Prescriptions.Update(prescription);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Prescription>> GetRecentAsync(Guid tenantId, int limit, CancellationToken ct = default)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Patient)
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Doctor)
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderByDescending(p => p.IssuedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<Prescription> CreateAsync(Prescription prescription, CancellationToken ct = default)
    {
        prescription.CreatedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync(ct);
        return prescription;
    }

    public async Task<Prescription> UpdateAsync(Prescription prescription, CancellationToken ct = default)
    {
        prescription.UpdatedAt = DateTime.UtcNow;
        _db.Prescriptions.Update(prescription);
        await _db.SaveChangesAsync(ct);
        return prescription;
    }

    public async Task IncrementPrintCountAsync(Guid id, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (prescription == null) return;

        prescription.PrintCount++;
        prescription.UpdatedAt = DateTime.UtcNow;
        _db.Prescriptions.Update(prescription);
        await _db.SaveChangesAsync(ct);
    }
}
