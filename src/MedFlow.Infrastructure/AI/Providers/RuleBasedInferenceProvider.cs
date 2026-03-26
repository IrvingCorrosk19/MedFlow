using MedFlow.Application.Interfaces.AI;
using MedFlow.Application.Interfaces.AI.Providers;

namespace MedFlow.Infrastructure.AI.Providers;

/// <summary>
/// Proveedor de inferencias basado en reglas heurísticas. No requiere modelos externos.
/// Produce resultados explicables con factores y evidencia.
/// </summary>
public sealed class RuleBasedInferenceProvider : IInferenceProvider
{
    private readonly INoShowRiskService _noShowRisk;
    private readonly IPaymentRiskService _paymentRisk;
    private readonly IPatientEngagementService _engagement;

    public RuleBasedInferenceProvider(
        INoShowRiskService noShowRisk,
        IPaymentRiskService paymentRisk,
        IPatientEngagementService engagement)
    {
        _noShowRisk = noShowRisk;
        _paymentRisk = paymentRisk;
        _engagement = engagement;
    }

    public string ProviderName => "RuleBased";

    public async Task<InferenceResult> InferAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return request.InferenceType.ToLowerInvariant() switch
            {
                "noshow" or "noshowrisk" => await InferNoShowAsync(request, cancellationToken),
                "payment" or "paymentrisk" => await InferPaymentAsync(request, cancellationToken),
                "engagement" => await InferEngagementAsync(request, cancellationToken),
                _ => new InferenceResult(false, 0, 0, string.Empty, null, [], null, $"Tipo de inferencia no soportado: {request.InferenceType}")
            };
        }
        catch (Exception ex)
        {
            return new InferenceResult(false, 0, 0, string.Empty, null, [], null, ex.Message);
        }
    }

    private async Task<InferenceResult> InferNoShowAsync(InferenceRequest request, CancellationToken ct)
    {
        if (!request.Inputs.TryGetValue("AppointmentId", out var aptObj) || aptObj is not Guid aptId)
            return new InferenceResult(false, 0, 0, string.Empty, null, [], null, "AppointmentId requerido");

        var r = await _noShowRisk.EvaluateAsync(aptId, ct);
        return new InferenceResult(true, r.Score, r.Confidence, r.Summary, r.Recommendation, r.Factors, null);
    }

    private async Task<InferenceResult> InferPaymentAsync(InferenceRequest request, CancellationToken ct)
    {
        if (!request.Inputs.TryGetValue("PatientId", out var pObj) || pObj is not Guid patientId)
            return new InferenceResult(false, 0, 0, string.Empty, null, [], null, "PatientId requerido");

        var r = await _paymentRisk.EvaluatePatientAsync(patientId, ct);
        var confidence = r.Score >= 70 ? 0.9m : r.Score >= 40 ? 0.8m : 0.7m;
        return new InferenceResult(true, r.Score, confidence, r.Summary, r.Recommendation, r.Factors, null);
    }

    private async Task<InferenceResult> InferEngagementAsync(InferenceRequest request, CancellationToken ct)
    {
        if (!request.Inputs.TryGetValue("PatientId", out var pObj) || pObj is not Guid patientId)
            return new InferenceResult(false, 0, 0, string.Empty, null, [], null, "PatientId requerido");

        var r = await _engagement.EvaluateAsync(patientId, ct);
        var factors = r.Factors.ToList();
        return new InferenceResult(true, r.Score, 0.85m, r.Summary, "Contactar para seguimiento y reactivación", factors, null);
    }
}
