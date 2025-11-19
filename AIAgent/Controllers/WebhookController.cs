using AIAgent.Jobs;
using AIAgent.Models;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace AIAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;
    
    public WebhookController(
        IBackgroundJobClient backgroundJobs,
        IConfiguration configuration,
        ILogger<WebhookController> logger)
    {
        _backgroundJobs = backgroundJobs;
        _configuration = configuration;
        _logger = logger;
    }
    
    // Верификация webhook (Meta требует это для активации)
    [HttpGet("meta")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var verifyToken = _configuration["Meta:VerifyToken"];
        
        if (mode == "subscribe" && token == verifyToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(challenge);
        }
        
        _logger.LogWarning("Webhook verification failed");
        return Forbid();
    }
    
    // Прием входящих сообщений
    [HttpPost("meta")]
    public IActionResult ReceiveMessage([FromBody] MetaWebhookDto webhook)
    {
        try
        {
            // Отправляем в фоновую обработку через Hangfire
            _backgroundJobs.Enqueue<MessageProcessingJob>(
                job => job.ProcessMessage(webhook));
            
            // Meta требует быстрый ответ 200 OK
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing webhook message");
            return StatusCode(500);
        }
    }
}