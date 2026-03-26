using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MedFlow.Application.Interfaces.Workflow;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedFlow.Infrastructure.Workflow;

public sealed class N8nWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IPayloadTemplateEngine _templateEngine;
    private readonly ILogger<N8nWorkflowDispatcher> _logger;

    public N8nWorkflowDispatcher(
        IHttpClientFactory httpFactory,
        IPayloadTemplateEngine templateEngine,
        ILogger<N8nWorkflowDispatcher> logger)
    {
        _httpFactory = httpFactory;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<DispatchResult> DispatchAsync(WorkflowExecution execution, WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(definition.WebhookUrl))
            return new DispatchResult(false, null, null, "Webhook URL is empty");

        // Validar la URL antes de dispatch para prevenir SSRF.
        // En producción se requiere HTTPS y se bloquean IPs/dominios privados.
        var urlValidation = WebhookUrlValidator.Validate(definition.WebhookUrl, requireHttpsInProduction: true);
        if (!urlValidation.IsValid)
        {
            _logger.LogWarning("Workflow {WorkflowId} has invalid webhook URL: {Error}", definition.Id, urlValidation.Error);
            return new DispatchResult(false, null, null, $"URL inválida: {urlValidation.Error}");
        }

        var variables = BuildVariablesFromPayload(execution.PayloadJson);
        var body = _templateEngine.Apply(definition.PayloadTemplateJson, variables);
        var method = string.IsNullOrWhiteSpace(definition.HttpMethod) ? HttpMethod.Post : new HttpMethod(definition.HttpMethod.ToUpperInvariant());
        var timeout = TimeSpan.FromSeconds(Math.Clamp(definition.TimeoutSeconds ?? 30, 5, 300));

        using var client = _httpFactory.CreateClient();
        client.Timeout = timeout;

        var request = new HttpRequestMessage(method, definition.WebhookUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(definition.HeadersJson))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(definition.HeadersJson);
                foreach (var (key, value) in headers ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(key) && request.Headers.TryAddWithoutValidation(key, value) == false)
                        request.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid headers JSON for workflow {WorkflowId}", definition.Id);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Workflow {WorkflowId} dispatch succeeded for execution {ExecutionId} in {Ms}ms",
                    definition.Id, execution.Id, sw.ElapsedMilliseconds);
                return new DispatchResult(true, (int)response.StatusCode, responseBody, null);
            }

            _logger.LogWarning("Workflow {WorkflowId} dispatch returned {StatusCode} for execution {ExecutionId}: {Body}",
                definition.Id, (int)response.StatusCode, execution.Id, responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody);
            return new DispatchResult(false, (int)response.StatusCode, responseBody, $"HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Workflow {WorkflowId} dispatch failed for execution {ExecutionId} after {Ms}ms",
                definition.Id, execution.Id, sw.ElapsedMilliseconds);
            return new DispatchResult(false, null, null, ex.Message);
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildVariablesFromPayload(string payloadJson)
    {
        var variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(payloadJson))
            return variables;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            FlattenToVariables(root, "", variables);
        }
        catch { /* ignore parse errors */ }

        return variables;
    }

    private static void FlattenToVariables(JsonElement element, string prefix, Dictionary<string, object?> variables)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenToVariables(prop.Value, path, variables);
                }
                break;
            case JsonValueKind.Array:
                variables[prefix] = GetValue(element);
                if (!string.IsNullOrEmpty(prefix)) variables[ToPascalCase(prefix)] = GetValue(element);
                break;
            default:
                variables[prefix] = GetValue(element);
                if (!string.IsNullOrEmpty(prefix)) variables[ToPascalCase(prefix)] = GetValue(element);
                break;
        }
    }

    private static string ToPascalCase(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        var parts = path.Split('.');
        return string.Concat(parts.Select(p => p.Length > 0 ? char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant() : ""));
    }

    private static object? GetValue(JsonElement e)
    {
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(),
            JsonValueKind.Number => e.TryGetInt64(out var i) ? i : e.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object or JsonValueKind.Array => e.GetRawText(),
            _ => e.GetRawText()
        };
    }
}
