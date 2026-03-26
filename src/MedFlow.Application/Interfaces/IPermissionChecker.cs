namespace MedFlow.Application.Interfaces;

public interface IPermissionChecker
{
    Task<bool> UserHasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UserIsInSuperAdminRoleAsync(string userId, CancellationToken cancellationToken = default);
}
