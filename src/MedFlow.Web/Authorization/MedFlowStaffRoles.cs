namespace MedFlow.Web.Authorization;

/// <summary>
/// Roles de personal clínico/administrativo. Excluye <see cref="Patient"/> (portal).
/// Usar en APIs y rutas que no deben exponerse al portal paciente autenticado.
/// </summary>
public static class MedFlowStaffRoles
{
    public const string List = "SuperAdmin,Admin,Reception,Doctor,Billing,Staff";
}
