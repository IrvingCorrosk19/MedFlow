using MedFlow.Application.Common;
using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces;

public interface IPatientService
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> GetAllAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 100,
        string? documento = null,
        string? telefono = null,
        int? edadDesde = null,
        int? edadHasta = null,
        CancellationToken cancellationToken = default);
    Task<PagedResult<Patient>> GetPagedAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);
    Task<(Patient? Patient, string? Error)> CreateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient> UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
