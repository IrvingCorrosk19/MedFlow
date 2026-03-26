using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MedFlow.Application.Interfaces;

namespace MedFlow.Infrastructure.Notifications;

public sealed class HttpClientWebhookSender : IWebhookSender
{
    private readonly IHttpClientFactory _httpFactory;

    public HttpClientWebhookSender(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<WebhookSendResult> SendAsync(string url, object payload, string method = "POST", CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                response = await client.GetAsync(url, cancellationToken);
            else if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                response = await client.PostAsync(url, content, cancellationToken);
            else if (string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase))
                response = await client.PutAsync(url, content, cancellationToken);
            else
                response = await client.PostAsync(url, content, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WebhookSendResult(response.IsSuccessStatusCode, body, response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new WebhookSendResult(false, null, ex.Message);
        }
    }
}
