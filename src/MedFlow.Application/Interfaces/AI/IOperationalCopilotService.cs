namespace MedFlow.Application.Interfaces.AI;

public interface IOperationalCopilotService
{
    Task<CopilotResponse> QueryAsync(Guid tenantId, string query, CancellationToken cancellationToken = default);
}

public record CopilotResponse(
    string Summary,
    IReadOnlyList<CopilotResponseItem> Items,
    IReadOnlyList<string> Suggestions);

public record CopilotResponseItem(string Title, string Description, string? EntityType, string? EntityId, string? ActionUrl);
