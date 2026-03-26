namespace MedFlow.Application.Interfaces.AI.Providers;

/// <summary>
/// Abstracción para conectividad con modelos de lenguaje o servicios predictivos externos.
/// Permite evolucionar hacia OpenAI, Azure OpenAI o modelos propios sin cambiar la arquitectura.
/// </summary>
public interface IAIModelProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Indica si el proveedor está disponible y configurado.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Genera completado de texto usando el modelo subyacente.
    /// Retorna null si el proveedor no está disponible o falla.
    /// </summary>
    Task<string?> CompleteAsync(string prompt, AIModelOptions? options = null, CancellationToken cancellationToken = default);
}

public record AIModelOptions(
    int? MaxTokens = null,
    decimal? Temperature = null,
    string? SystemPrompt = null);
