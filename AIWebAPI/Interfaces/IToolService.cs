using AIWebAPI.Persistence.Entities;

namespace AIWebAPI.Interfaces;

public interface IToolService
{
    Task<List<Tool>> GetAllToolsAsync();
    Task<Tool> GetToolByIdAsync(Guid id);
    Task<List<Tool>> GetToolsByIdsAsync(List<Guid> ids);
    Task<List<Tool>> SearchToolsAsync(string category, string name, decimal maxPrice);
    Task<Tool> CreateToolAsync(Tool tool);
    Task<Tool> UpdateToolAsync(Guid id, Tool tool);
    Task<bool> DeleteToolAsync(Guid id);
    Task<Dictionary<Guid, bool>> CheckAvailabilityAsync(List<Guid> toolIds);
    Task<List<ToolUsageInstruction>> GetInstructionsAsync(Guid toolId);
    Task<List<Tool>> GetToolsByCategoryAsync(string category);
    Task SyncEmbeddingsAsync(bool forceUpdate = false);
}