using System.Text.Json;
using System.Text.RegularExpressions;
using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MedFlow.Web.Controllers.Api;

[ApiController]
[Route("api/integrations/webhooks/n8n")]
public class N8nWebhooksController : ControllerBase
{
    // Solo letras, números, guiones y puntos. Máximo 64 caracteres.
    // Previene log injection y nombres de evento arbitrarios.
    private static readonly Regex ValidEventType = new(
        @"^[A-Za-z0-9][A-Za-z0-9\-\.]{0,63}$",
        RegexOptions.Compiled);

    private readonly IConfiguration _config;
    private readonly IEventLogService _events;

    public N8nWebhooksController(IConfiguration config, IEventLogService events)
    {
        _config = config;
        _events = events;
    }

    [HttpPost("{eventType}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Receive(string eventType, [FromBody] JsonElement? body, CancellationToken cancellationToken)
    {
        var expected = _config["Integrations:N8n:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
            return StatusCode(503, new { error = "Integración no configurada." });

        var key = Request.Headers["X-Api-Key"].FirstOrDefault()
                  ?? Request.Headers["X-N8n-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(key) || key != expected)
            return Unauthorized();

        // Validar el nombre del evento para evitar log injection y nombres arbitrarios.
        if (string.IsNullOrWhiteSpace(eventType) || !ValidEventType.IsMatch(eventType))
            return BadRequest(new { error = "eventType inválido. Solo se permiten letras, números, guiones y puntos (máx. 64 caracteres)." });

        object payload = body is JsonElement b && b.ValueKind != JsonValueKind.Undefined
            ? JsonSerializer.Deserialize<object>(b.GetRawText()) ?? new { }
            : new { };

        await _events.EnqueueAsync($"Webhook.N8n.{eventType}", payload, "N8nWebhook", eventType, cancellationToken);
        return Accepted(new { received = true, eventType });
    }
}
