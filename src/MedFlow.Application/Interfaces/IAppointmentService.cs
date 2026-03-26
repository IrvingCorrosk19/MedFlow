using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IAppointmentService
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAllAsync(DateTime? from = null, DateTime? to = null, Guid? doctorId = null, Guid? patientId = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid doctorId, DateTime date, TimeSpan start, TimeSpan end, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
