using System.Text.Json;
using AIAgent.Interfaces;
using AIAgent.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace AIAgent.Services;

// Services/Interfaces/IConversationContextService.cs
public interface IConversationContextService
{
    Task<ConversationContext> GetContextAsync(string userId, string platform);
    Task SaveContextAsync(ConversationContext context);
    Task AddMessageAsync(string userId, string platform, ChatMessage message);
    Task ClearContextAsync(string userId, string platform);
}

// Services/Context/ConversationContextService.cs
public class ConversationContextService : IConversationContextService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConversationContextService> _logger;
    private readonly TimeSpan _contextExpiration = TimeSpan.FromHours(24);
    
    public ConversationContextService(
        IDistributedCache cache,
        ILogger<ConversationContextService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<ConversationContext> GetContextAsync(string userId, string platform)
    {
        var key = GetCacheKey(userId, platform);
        var cachedData = await _cache.GetStringAsync(key);
        
        if (string.IsNullOrEmpty(cachedData))
        {
            return new ConversationContext
            {
                UserId = userId,
                Platform = platform,
                LastActivity = DateTime.UtcNow
            };
        }
        
        return JsonSerializer.Deserialize<ConversationContext>(cachedData);
    }
    
    public async Task SaveContextAsync(ConversationContext context)
    {
        var key = GetCacheKey(context.UserId, context.Platform);
        context.LastActivity = DateTime.UtcNow;
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _contextExpiration
        };
        
        var serialized = JsonSerializer.Serialize(context);
        await _cache.SetStringAsync(key, serialized, options);
    }
    
    public async Task AddMessageAsync(string userId, string platform, ChatMessage message)
    {
        var context = await GetContextAsync(userId, platform);
        context.Messages.Add(message);
        
        // Ограничиваем историю последними 10 сообщениями
        if (context.Messages.Count > 10)
        {
            context.Messages = context.Messages.TakeLast(10).ToList();
        }
        
        await SaveContextAsync(context);
    }
    
    public async Task ClearContextAsync(string userId, string platform)
    {
        var key = GetCacheKey(userId, platform);
        await _cache.RemoveAsync(key);
    }
    
    private string GetCacheKey(string userId, string platform) 
        => $"conversation:{platform}:{userId}";
}