using MedFlow.Application.Common;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IDoctorService
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetAllAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<PagedResult<Doctor>> GetPagedAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<(Doctor? Doctor, string? Error)> CreateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task<Doctor> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
