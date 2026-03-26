namespace MedFlow.Infrastructure.AI.Providers;

/// <summary>
/// Proveedor nulo para modelos externos. Usado cuando no hay LLM configurado.
/// La arquitectura permite intercambiar por OpenAI/Azure en el futuro.
/// </summary>
public sealed class NullAIModelProvider : Application.Interfaces.AI.Providers.IAIModelProvider
{
    public string ProviderName => "Null";

    public bool IsAvailable => false;

    public Task<string?> CompleteAsync(string prompt, Application.Interfaces.AI.Providers.AIModelOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
