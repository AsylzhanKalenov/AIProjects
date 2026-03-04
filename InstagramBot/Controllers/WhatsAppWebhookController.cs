using Microsoft.AspNetCore.Mvc;
using InstagramBot.Models.WhatsApp;
using InstagramBot.Services;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppMessageHandler _handler;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppMessageHandler handler,
        IConfiguration config,
        ILogger<WhatsAppWebhookController> logger)
    {
        _handler = handler;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// WhatsApp webhook verification (called by Meta during webhook setup)
    /// </summary>
    [HttpGet("whatsapp")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        _logger.LogInformation("WhatsApp webhook verification: mode={Mode}", mode);

        // Uses the same verify token as Instagram (can be separated if needed)
        var verifyToken = _config["Meta:VerifyToken"];

        if (mode == "subscribe" && token == verifyToken)
        {
            _logger.LogInformation("WhatsApp webhook verified successfully");
            return Ok(challenge);
        }

        _logger.LogWarning("WhatsApp webhook verification failed");
        return Forbid();
    }

    /// <summary>
    /// Incoming WhatsApp messages endpoint
    /// </summary>
    [HttpPost("whatsapp")]
    public IActionResult HandleMessage([FromBody] WhatsAppWebhookPayload payload)
    {
        _logger.LogInformation(
            "Received WhatsApp webhook: object={Object}, entries={Count}",
            payload.Object, payload.Entry?.Count ?? 0);

        // Meta expects quick 200 response, process asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _handler.ProcessAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp webhook payload");
            }
        });

        return Ok();
    }
}
