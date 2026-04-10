using MedFlow.Application.Interfaces.AI.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedFlow.Infrastructure.AI.Providers;

/// <summary>
/// AI model provider using the Anthropic Claude API (claude-haiku-4-5 by default for cost efficiency).
/// Configure via appsettings: Anthropic:ApiKey and Anthropic:Model.
/// </summary>
public sealed class AnthropicAIModelProvider : IAIModelProvider
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicAIModelProvider> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public string ProviderName => "Anthropic Claude";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_apiKey);

    public AnthropicAIModelProvider(
        HttpClient http,
        IConfiguration configuration,
        ILogger<AnthropicAIModelProvider> logger)
    {
        _http = http;
        _apiKey = configuration["Anthropic:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        _model = configuration["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
        _logger = logger;

        _http.BaseAddress = new Uri("https://api.anthropic.com/");
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey ?? "");
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string?> CompleteAsync(
        string prompt,
        AIModelOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable) return null;

        try
        {
            var messages = new List<object>
            {
                new { role = "user", content = prompt }
            };

            var requestBody = new
            {
                model = _model,
                max_tokens = options?.MaxTokens ?? 1024,
                system = options?.SystemPrompt ?? "Eres un asistente médico inteligente para la clínica. Responde siempre en español, de forma concisa y profesional.",
                messages
            };

            var response = await _http.PostAsJsonAsync(
                "v1/messages", requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Anthropic API error {Status}: {Error}", response.StatusCode, err);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(
                JsonOpts, cancellationToken);

            return result?.Content?.FirstOrDefault()?.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API.");
            return null;
        }
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
