namespace MedFlow.Application.Interfaces.AI;

public interface INoShowRiskService
{
    Task<NoShowRiskResult> EvaluateAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}

public record NoShowRiskResult(
    decimal Score,
    string RiskLevel,
    string Summary,
    string Recommendation,
    IReadOnlyList<string> Factors,
    decimal Confidence);
