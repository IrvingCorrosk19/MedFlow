using MedFlow.Domain.Enums;

namespace MedFlow.Application.Interfaces.AI;

public interface IPatientEngagementService
{
    Task<PatientEngagementResult> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public record PatientEngagementResult(
    PatientEngagementLevel Level,
    decimal Score,
    string Summary,
    IReadOnlyList<string> Factors,
    DateTime? LastAppointmentDate,
    int AppointmentsLast90Days,
    bool HasPortalAccess,
    bool RespondsToReminders);
