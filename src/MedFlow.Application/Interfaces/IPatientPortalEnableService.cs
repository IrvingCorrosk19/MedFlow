namespace MedFlow.Application.Interfaces;

public interface IPatientPortalEnableService
{
    Task<(bool Success, string? UserId, string? TemporaryPassword, string? Error)> EnablePortalForPatientAsync(Guid patientId, string? preferredPassword, CancellationToken cancellationToken = default);
    Task<bool> DisablePortalForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
}
