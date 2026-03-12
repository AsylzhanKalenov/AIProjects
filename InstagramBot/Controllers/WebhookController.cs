using System.Text.Json;
using InstagramBot.Handler;
using Microsoft.AspNetCore.Mvc;
using InstagramBot.Models;
using InstagramBot.Services;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<WebhookController> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Webhook verification endpoint (called by Meta when setting up webhook)
    /// </summary>
    [HttpGet("instagram")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        _logger.LogInformation("Webhook verification request: mode={Mode}", mode);

        var verifyToken = _config["Meta:VerifyToken"];

        if (mode == "subscribe" && token == verifyToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(challenge);
        }

        _logger.LogWarning("Webhook verification failed: invalid token");
        return Forbid();
    }

    /// <summary>
    /// Incoming messages endpoint
    /// </summary>
    [HttpPost("instagram")]
    public IActionResult HandleMessage([FromBody] JsonElement payload)
    {
        var rawJson = payload.ToString();

        _logger.LogInformation("Received webhook payload: {Payload}", rawJson);
        // Meta expects quick response, process asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                WebhookPayload payloadd =  JsonSerializer.Deserialize<WebhookPayload>(rawJson);
                
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
                await handler.ProcessAsync(payloadd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook payload");
            }
        });

        // Always return OK quickly to Meta
        return Ok();
    }
}
