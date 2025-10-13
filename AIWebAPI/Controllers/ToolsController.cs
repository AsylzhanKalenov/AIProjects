using AIWebAPI.Interfaces;
using AIWebAPI.Models;
using AIWebAPI.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AIWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController: ControllerBase
{
    private readonly IToolService _toolService;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(IToolService toolService, ILogger<ToolsController> logger)
    {
        _toolService = toolService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Tool>>> GetTools()
    {
        var tools = await _toolService.GetAllToolsAsync();
        return Ok(tools);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Tool>> GetTool(Guid id)
    {
        var tool = await _toolService.GetToolByIdAsync(id);
        if (tool == null)
        {
            return NotFound();
        }
        return Ok(tool);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Tool>>> SearchTools(
        [FromQuery] string category = null,
        [FromQuery] string name = null,
        [FromQuery] decimal maxPrice = 0)
    {
        var tools = await _toolService.SearchToolsAsync(category, name, maxPrice);
        return Ok(tools);
    }

    [HttpPost]
    public async Task<ActionResult<Tool>> CreateTool([FromBody] Tool tool)
    {
        var createdTool = await _toolService.CreateToolAsync(tool);
        return CreatedAtAction(nameof(GetTool), new { id = createdTool.Id }, createdTool);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Tool>> UpdateTool(Guid id, [FromBody] Tool tool)
    {
        var updatedTool = await _toolService.UpdateToolAsync(id, tool);
        if (updatedTool == null)
        {
            return NotFound();
        }
        return Ok(updatedTool);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTool(Guid id)
    {
        var deleted = await _toolService.DeleteToolAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{id}/instructions")]
    public async Task<ActionResult<List<ToolUsageInstruction>>> GetInstructions(Guid id)
    {
        var instructions = await _toolService.GetInstructionsAsync(id);
        return Ok(instructions);
    }

    [HttpPost("sync-embeddings")]
    public async Task<ActionResult> SyncEmbeddings([FromBody] SyncRequest request)
    {
        await _toolService.SyncEmbeddingsAsync(request.ForceUpdate);
        return Ok(new { message = "Embeddings sync started" });
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<List<Tool>>> GetByCategory(string category)
    {
        var tools = await _toolService.GetToolsByCategoryAsync(category);
        return Ok(tools);
    }
}