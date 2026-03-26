namespace MedFlow.Application.Interfaces.AI;

public interface IOperationalSummaryService
{
    Task<OperationalSummaryDto> GenerateDailySummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default);
}

public record OperationalSummaryDto(
    DateTime Date,
    string Summary,
    int RisksCount,
    int RecommendationsCount,
    IReadOnlyList<OperationalSummarySection> Sections);

public record OperationalSummarySection(string Title, string Content, int Count);
