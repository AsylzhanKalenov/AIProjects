using System.Text.Json;
using AIWebAPI.Interfaces;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace AIWebAPI.Services;

public class FunctionToolService : IFunctionToolService
{
    private readonly IToolService _toolService;
    private readonly ILogger<FunctionToolService> _logger;

    public FunctionToolService(IToolService toolService, ILogger<FunctionToolService> logger)
    {
        _toolService = toolService;
        _logger = logger;
    }

    public async Task<string> ExecuteFunctionAsync(FunctionCall functionCall)
    {
        _logger.LogInformation($"Executing function: {functionCall.Name}");
        
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            functionCall.Arguments);

        return functionCall.Name switch
        {
            "search_tools" => await SearchTools(parameters),
            "get_tool_details" => await GetToolDetails(parameters),
            "check_availability" => await CheckAvailability(parameters),
            "get_usage_instructions" => await GetUsageInstructions(parameters),
            "compare_tools" => await CompareTools(parameters),
            _ => throw new NotImplementedException($"Function {functionCall.Name} not implemented")
        };
    }

    private async Task<string> SearchTools(Dictionary<string, JsonElement> parameters)
    {
        var category = parameters.ContainsKey("category") 
            ? parameters["category"].GetString() : null;
        var name = parameters.ContainsKey("name") 
            ? parameters["name"].GetString() : null;
        var maxPrice = parameters.ContainsKey("max_price") 
            ? parameters["max_price"].GetDecimal() : decimal.MaxValue;

        var tools = await _toolService.SearchToolsAsync(category, name, maxPrice);
        
        return JsonSerializer.Serialize(tools.Select(t => new
        {
            t.Id,
            t.Name,
            t.Category,
            t.Price,
            t.Availability
        }));
    }

    private async Task<string> GetToolDetails(Dictionary<string, JsonElement> parameters)
    {
        var toolId = Guid.Parse(parameters["tool_id"].GetString());
        var tool = await _toolService.GetToolByIdAsync(toolId);
        
        return JsonSerializer.Serialize(tool);
    }

    private async Task<string> CheckAvailability(Dictionary<string, JsonElement> parameters)
    {
        var toolIds = parameters["tool_ids"]
            .EnumerateArray()
            .Select(e => Guid.Parse(e.GetString()))
            .ToList();

        var availability = await _toolService.CheckAvailabilityAsync(toolIds);
        
        return JsonSerializer.Serialize(availability);
    }

    private async Task<string> GetUsageInstructions(Dictionary<string, JsonElement> parameters)
    {
        var toolId = Guid.Parse(parameters["tool_id"].GetString());
        var instructions = await _toolService.GetInstructionsAsync(toolId);
        
        return JsonSerializer.Serialize(instructions);
    }

    private async Task<string> CompareTools(Dictionary<string, JsonElement> parameters)
    {
        var toolIds = parameters["tool_ids"]
            .EnumerateArray()
            .Select(e => Guid.Parse(e.GetString()))
            .ToList();

        var tools = await _toolService.GetToolsByIdsAsync(toolIds);
        
        var comparison = tools.Select(t => new
        {
            t.Name,
            t.Category,
            t.Price,
            t.Manufacturer,
            Features = t.Specifications
        });

        return JsonSerializer.Serialize(comparison);
    }
}