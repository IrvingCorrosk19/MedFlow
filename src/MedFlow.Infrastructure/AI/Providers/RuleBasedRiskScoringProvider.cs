using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Interfaces.AI.Providers;

namespace MedFlow.Infrastructure.AI.Providers;

/// <summary>
/// Proveedor de riesgo basado en reglas heurísticas. Delega a los servicios existentes.
/// Base para evolución hacia modelos ML externos.
/// </summary>
public sealed class RuleBasedRiskScoringProvider : IRiskScoringProvider
{
    private readonly INoShowRiskService _noShowRisk;
    private readonly IPaymentRiskService _paymentRisk;
    private readonly IPatientEngagementService _engagement;

    public RuleBasedRiskScoringProvider(
        INoShowRiskService noShowRisk,
        IPaymentRiskService paymentRisk,
        IPatientEngagementService engagement)
    {
        _noShowRisk = noShowRisk;
        _paymentRisk = paymentRisk;
        _engagement = engagement;
    }

    public string ProviderName => "RuleBased";

    public bool SupportsNoShow => true;
    public bool SupportsPaymentRisk => true;
    public bool SupportsEngagement => true;

    public Task<NoShowRiskResult> EvaluateNoShowAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        => _noShowRisk.EvaluateAsync(appointmentId, cancellationToken);

    public Task<PaymentRiskResult> EvaluatePaymentRiskAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _paymentRisk.EvaluatePatientAsync(patientId, cancellationToken);

    public Task<PatientEngagementResult> EvaluateEngagementAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _engagement.EvaluateAsync(patientId, cancellationToken);
}
