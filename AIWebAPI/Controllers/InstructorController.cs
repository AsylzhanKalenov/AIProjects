using AIWebAPI.Interfaces;
using AIWebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly ILogger<InstructorController> _logger;

    public InstructorController(ILLMService llmService, ILogger<InstructorController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> ProcessQuery([FromBody] QueryRequest request)
    {
        try
        {
            _logger.LogInformation($"Processing query: {request.Query}");
            
            var response = await _llmService.GenerateResponseAsync(request);
            
            return Ok(new QueryResponse
            {
                Success = true,
                Response = response,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            return StatusCode(500, new QueryResponse
            {
                Success = false,
                Error = "An error occurred processing your request",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("webhook/n8n")]
    public async Task<IActionResult> N8nWebhook([FromBody] N8nWebhookRequest request)
    {
        // Process n8n webhook
        var queryRequest = new QueryRequest
        {
            Query = request.Message,
            UserId = request.UserId,
            Context = request.Context
        };

        var response = await _llmService.GenerateResponseAsync(queryRequest);
        
        return Ok(new
        {
            response,
            metadata = new
            {
                processed_at = DateTime.UtcNow,
                workflow_id = request.WorkflowId
            }
        });
    }
}