namespace MedFlow.Application.Interfaces;

/// <summary>
/// Alcance clínico por perfil: los usuarios solo-médico ven pacientes/citas/historias ligadas a su <see cref="Doctor"/> vinculado (UserId).
/// </summary>
public interface IClinicalUserScope
{
    /// <summary>
    /// Si <paramref name="RestrictToDoctor"/> es true, aplicar filtros por médico vinculado.
    /// Si además <paramref name="LinkedDoctorId"/> es null, el usuario tiene rol Doctor pero sin ficha vinculada: no debe ver datos clínicos.
    /// </summary>
    Task<(bool RestrictToDoctor, Guid? LinkedDoctorId)> GetDoctorDataScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Paciente atendido por el médico mediante al menos una cita o nota clínica.
    /// </summary>
    Task<bool> DoctorHasClinicalRelationshipWithPatientAsync(Guid doctorId, Guid patientId, CancellationToken cancellationToken = default);
}
