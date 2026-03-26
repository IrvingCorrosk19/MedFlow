using System.Text;
using System.Text.Json;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Interfaces.Workflow;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Infrastructure.Workflow;

public sealed class WorkflowTestService : IWorkflowTestService
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ITenantContext _tenant;

    public WorkflowTestService(
        IApplicationDbContext context,
        IHttpClientFactory httpFactory,
        ITenantContext tenant)
    {
        _context = context;
        _httpFactory = httpFactory;
        _tenant = tenant;
    }

    public async Task<WorkflowTestResult> TestWebhookAsync(Guid workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var def = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == workflowDefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow not found");

        if (string.IsNullOrWhiteSpace(def.WebhookUrl))
            return new WorkflowTestResult(false, null, null, "Webhook URL vacío");

        var testPayload = new Dictionary<string, object?>
        {
            ["eventType"] = def.TriggerEvent,
            ["aggregateId"] = "test-" + Guid.NewGuid().ToString("N")[..8],
            ["test"] = true,
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
            ["tenantId"] = _tenant.TenantId?.ToString()
        };

        var body = JsonSerializer.Serialize(testPayload);
        var method = string.IsNullOrWhiteSpace(def.HttpMethod) ? HttpMethod.Post : new HttpMethod(def.HttpMethod.ToUpperInvariant());

        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        var request = new HttpRequestMessage(method, def.WebhookUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WorkflowTestResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
                response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new WorkflowTestResult(false, null, null, ex.Message);
        }
    }
}
