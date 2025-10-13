using System.Text.Json;
using AIWebAPI.Interfaces;
using AIWebAPI.Persistence.Contexts;
using AIWebAPI.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AIWebAPI.Services;

public class ToolService : IToolService
    {
        private readonly ToolDbContext _context;
        private readonly IVectorService _vectorService;
        private readonly ILLMService _llmService;
        private readonly ILogger<ToolService> _logger;
        private readonly IDistributedCache _cache;

        public ToolService(
            ToolDbContext context,
            IVectorService vectorService,
            ILLMService llmService,
            ILogger<ToolService> logger,
            IDistributedCache cache)
        {
            _context = context;
            _vectorService = vectorService;
            _llmService = llmService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<Tool>> GetAllToolsAsync()
        {
            var cacheKey = "all_tools";
            var cachedTools = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedTools))
            {
                return JsonSerializer.Deserialize<List<Tool>>(cachedTools);
            }

            var tools = await _context.Tools
                .Include(t => t.Instructions)
                .ToListAsync();
            
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(tools),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });

            return tools;
        }

        public async Task<Tool> GetToolByIdAsync(Guid id)
        {
            var cacheKey = $"tool_{id}";
            var cachedTool = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedTool))
            {
                return JsonSerializer.Deserialize<Tool>(cachedTool);
            }

            var tool = await _context.Tools
                .Include(t => t.Instructions)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (tool != null)
            {
                await _cache.SetStringAsync(
                    cacheKey, 
                    JsonSerializer.Serialize(tool),
                    new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(10)
                    });
            }

            return tool;
        }

        public async Task<List<Tool>> GetToolsByIdsAsync(List<Guid> ids)
        {
            return await _context.Tools
                .Where(t => ids.Contains(t.Id))
                .Include(t => t.Instructions)
                .ToListAsync();
        }

        public async Task<List<Tool>> SearchToolsAsync(string category, string name, decimal maxPrice)
        {
            var query = _context.Tools.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category.ToLower().Contains(category.ToLower()));
            }

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(t => t.Name.ToLower().Contains(name.ToLower()) ||
                                        t.Description.ToLower().Contains(name.ToLower()));
            }

            if (maxPrice > 0)
            {
                query = query.Where(t => t.Price <= maxPrice);
            }

            return await query
                .Include(t => t.Instructions)
                .OrderBy(t => t.Price)
                .Take(20)
                .ToListAsync();
        }

        public async Task<Tool> CreateToolAsync(Tool tool)
        {
            tool.Id = Guid.NewGuid();
            tool.CreatedAt = DateTime.UtcNow;
            tool.UpdatedAt = DateTime.UtcNow;

            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();

            // Create embedding for the new tool
            await CreateToolEmbeddingAsync(tool);

            // Invalidate cache
            await _cache.RemoveAsync("all_tools");

            return tool;
        }

        public async Task<Tool> UpdateToolAsync(Guid id, Tool tool)
        {
            var existingTool = await _context.Tools.FindAsync(id);
            if (existingTool == null)
            {
                return null;
            }

            existingTool.Name = tool.Name;
            existingTool.Category = tool.Category;
            existingTool.Description = tool.Description;
            existingTool.Specifications = tool.Specifications;
            existingTool.Manufacturer = tool.Manufacturer;
            existingTool.ModelNumber = tool.ModelNumber;
            existingTool.Price = tool.Price;
            existingTool.Availability = tool.Availability;
            existingTool.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update embedding
            await UpdateToolEmbeddingAsync(existingTool);

            // Invalidate cache
            await _cache.RemoveAsync($"tool_{id}");
            await _cache.RemoveAsync("all_tools");

            return existingTool;
        }

        public async Task<bool> DeleteToolAsync(Guid id)
        {
            var tool = await _context.Tools.FindAsync(id);
            if (tool == null)
            {
                return false;
            }

            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();

            // Remove from vector database
            await _vectorService.DeleteAsync(id.ToString());

            // Invalidate cache
            await _cache.RemoveAsync($"tool_{id}");
            await _cache.RemoveAsync("all_tools");

            return true;
        }

        public async Task<Dictionary<Guid, bool>> CheckAvailabilityAsync(List<Guid> toolIds)
        {
            var tools = await _context.Tools
                .Where(t => toolIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Availability })
                .ToListAsync();

            return tools.ToDictionary(t => t.Id, t => t.Availability);
        }

        public async Task<List<ToolUsageInstruction>> GetInstructionsAsync(Guid toolId)
        {
            return await _context.ToolUsageInstructions
                .Where(i => i.ToolId == toolId)
                .OrderBy(i => i.StepNumber)
                .ToListAsync();
        }

        public async Task<List<Tool>> GetToolsByCategoryAsync(string category)
        {
            return await _context.Tools
                .Where(t => t.Category == category)
                .Include(t => t.Instructions)
                .ToListAsync();
        }

        public async Task SyncEmbeddingsAsync(bool forceUpdate = false)
        {
            var tools = await _context.Tools.ToListAsync();
            var updatedCount = 0;

            foreach (var tool in tools)
            {
                var exists = await _vectorService.ExistsAsync(tool.Id.ToString());
                
                if (!exists || forceUpdate)
                {
                    await CreateToolEmbeddingAsync(tool);
                    updatedCount++;
                    _logger.LogInformation($"Updated embedding for tool: {tool.Name}");
                }
            }

            _logger.LogInformation($"Sync completed. Updated {updatedCount} embeddings.");
        }

        private async Task CreateToolEmbeddingAsync(Tool tool)
        {
            var text = $"{tool.Name} {tool.Category} {tool.Description} {tool.Manufacturer}";
            var embedding = await _llmService.GenerateEmbeddingAsync(text);
            
            await _vectorService.UpsertAsync(
                id: tool.Id.ToString(),
                vector: embedding,
                metadata: new Dictionary<string, object>
                {
                    ["name"] = tool.Name,
                    ["category"] = tool.Category,
                    ["price"] = tool.Price,
                    ["availability"] = tool.Availability
                });
        }

        private async Task UpdateToolEmbeddingAsync(Tool tool)
        {
            await CreateToolEmbeddingAsync(tool); // Upsert handles updates
        }
    }