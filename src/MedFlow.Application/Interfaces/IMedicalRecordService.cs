using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecord?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalRecord>> GetHistoryByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalRecord>> SearchAsync(string query, Guid? patientId = null, Guid? doctorId = null, CancellationToken cancellationToken = default);
    Task<MedicalRecord> CreateAsync(MedicalRecord record, IReadOnlyList<Prescription>? prescriptions, CancellationToken cancellationToken = default);
    Task<MedicalRecord> UpdateAsync(MedicalRecord record, IReadOnlyList<Prescription>? prescriptions, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
