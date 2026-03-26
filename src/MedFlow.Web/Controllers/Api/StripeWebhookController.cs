using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace MedFlow.Web.Controllers.Api;

[ApiController]
[Route("api/billing/stripe")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly IStripeWebhookProcessor _processor;
    private readonly IBillingProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IStripeWebhookProcessor processor,
        IBillingProvider provider,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<StripeWebhookController> logger)
    {
        _processor = processor;
        _provider = provider;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string payload;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var sig = Request.Headers["Stripe-Signature"].FirstOrDefault();

        var secret = _configuration["Stripe:WebhookSecret"] ?? "";
        var isDevelopment = _hostEnvironment.IsDevelopment();

        // En producción debemos validar la firma siempre.
        // En Development permitimos webhooks de prueba cuando el secret está vacío.
        if (string.IsNullOrEmpty(sig))
        {
            if (!isDevelopment || !string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                return BadRequest();
            }

            _logger.LogWarning("Skipping Stripe signature validation in Development: missing signature and empty secret");
        }
        else if (!string.IsNullOrEmpty(secret))
        {
            if (!_provider.ValidateWebhookSignature(payload, sig, secret))
            {
                _logger.LogWarning("Stripe webhook signature validation failed");
                return BadRequest();
            }
        }
        else
        {
            // Secret vacío => no validamos firma (Development únicamente).
            if (!isDevelopment)
            {
                _logger.LogWarning("Stripe webhook secret is empty (non-Development), refusing request");
                return BadRequest();
            }

            _logger.LogWarning("Skipping Stripe signature validation in Development: empty secret");
        }

        try
        {
            await _processor.ProcessAsync(payload, sig ?? string.Empty, cancellationToken);
            return Ok();
        }
        catch (Stripe.StripeException ex)
        {
            // Si el procesador falla por firma inválida o payload malformado,
            // retornamos 400 para que Stripe no reintente indefinidamente.
            _logger.LogWarning(ex, "Stripe webhook: error de firma o payload inválido en el procesador");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed");
            return StatusCode(500);
        }
    }
}
