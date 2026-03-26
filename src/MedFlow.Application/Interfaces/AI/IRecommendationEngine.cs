namespace MedFlow.Application.Interfaces.AI;

public interface IRecommendationEngine
{
    Task<IReadOnlyList<AIRecommendation>> GenerateRecommendationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record AIRecommendation(
    string Type,
    string Title,
    string Description,
    string SuggestedAction,
    string? EntityType,
    string? EntityId,
    string? ActionUrl,
    int Priority,
    string? EvidenceJson,
    string Severity);
