using InstagramBot.Data;
using InstagramBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/webhooks")]
public class OpenAIController : Controller
{
    private readonly IOpenAiService _openAiService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OpenAIController> _logger;

    public OpenAIController(IOpenAiService openAiService, AppDbContext dbContext, ILogger<OpenAIController> logger)
    {
        _openAiService = openAiService;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    
    [HttpPost("test")]
    public async Task<IActionResult> Verify(string message)
    {
        var tenant = _dbContext.Tenants.FirstOrDefault(x => x.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        
        var response = await _openAiService.GetResponseAsync(tenant.SystemPrompt, tenant.KnowledgeBase, message);
        
        if (!string.IsNullOrEmpty(response))
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(response);
        }

        _logger.LogWarning("Webhook verification failed: invalid token");
        return Forbid();
    }
}