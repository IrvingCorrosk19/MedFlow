using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IPrescriptionService
{
    Task<IReadOnlyList<Prescription>> GetByMedicalRecordAsync(Guid medicalRecordId, CancellationToken ct = default);
    Task<IReadOnlyList<Prescription>> GetByPatientAsync(Guid patientId, Guid tenantId, CancellationToken ct = default);
    Task<Prescription?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task VoidAsync(Guid id, Guid tenantId, string reason, CancellationToken ct = default);
    Task IncrementPrintCountAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Prescription>> GetRecentAsync(Guid tenantId, int limit, CancellationToken ct = default);
    Task<Prescription> CreateAsync(Prescription prescription, CancellationToken ct = default);
    Task<Prescription> UpdateAsync(Prescription prescription, CancellationToken ct = default);
}
