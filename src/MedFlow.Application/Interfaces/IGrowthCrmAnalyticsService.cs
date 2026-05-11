namespace MedFlow.Application.Interfaces;

public sealed record PatientEngagementSummaryRow(Guid PatientId, string PatientName, int AppointmentCount);

public interface IGrowthCrmAnalyticsService
{
    /// <summary>
    /// Top pacientes por volumen de citas en el intervalo (fecha de cita).
    /// </summary>
    Task<IReadOnlyList<PatientEngagementSummaryRow>> GetTopPatientsByAppointmentVolumeAsync(
        Guid tenantId,
        int lastDays,
        int take,
        CancellationToken cancellationToken = default);
}
