using MedFlow.Application.Interfaces.AI;

namespace MedFlow.Application.Interfaces.AI.Providers;

/// <summary>
/// Proveedor unificado de scoring de riesgos. Permite implementaciones rule-based o basadas en ML.
/// La arquitectura queda lista para conectar modelos predictivos externos.
/// </summary>
public interface IRiskScoringProvider
{
    string ProviderName { get; }

    bool SupportsNoShow { get; }
    bool SupportsPaymentRisk { get; }
    bool SupportsEngagement { get; }

    Task<NoShowRiskResult> EvaluateNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<PaymentRiskResult> EvaluatePaymentRiskAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientEngagementResult> EvaluateEngagementAsync(Guid patientId, CancellationToken cancellationToken = default);
}
