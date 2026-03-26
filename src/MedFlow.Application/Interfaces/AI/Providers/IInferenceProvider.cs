namespace MedFlow.Application.Interfaces.AI.Providers;

/// <summary>
/// Proveedor de inferencias estructuradas. Permite respuestas explicables con score, confianza y evidencia.
/// Puede implementarse con reglas heurísticas o con modelos externos.
/// </summary>
public interface IInferenceProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Ejecuta una inferencia estructurada con inputs y devuelve resultado explicable.
    /// </summary>
    Task<InferenceResult> InferAsync(InferenceRequest request, CancellationToken cancellationToken = default);
}

public record InferenceRequest(
    string InferenceType,
    IReadOnlyDictionary<string, object> Inputs,
    Guid? TenantId = null);

public record InferenceResult(
    bool Success,
    decimal Score,
    decimal Confidence,
    string Summary,
    string? Recommendation,
    IReadOnlyList<string> Factors,
    string? EvidenceJson,
    string? ErrorMessage = null);
