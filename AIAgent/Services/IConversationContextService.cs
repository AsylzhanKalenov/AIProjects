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

// Services/Interfaces/IAIService.cs
public interface IAIService
{
    Task<string> GenerateResponseAsync(string userMessage, ConversationContext context);
}

// Services/AI/OpenAIService.cs
public class OpenAIService : IAIService
{
    private readonly IOpenAIClient _client;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly string _systemPrompt;
    
    public OpenAIService(
        IOpenAIClient client,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _client = client;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"];
        _systemPrompt = configuration["OpenAI:SystemPrompt"] ?? 
            "Ты дружелюбный AI-ассистент. Отвечай кратко и по делу.";
    }
    
    public async Task<string> GenerateResponseAsync(
        string userMessage, 
        ConversationContext context)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = _systemPrompt }
            };
            
            // Добавляем историю
            messages.AddRange(context.Messages);
            
            // Добавляем текущее сообщение
            messages.Add(new ChatMessage 
            { 
                Role = "user", 
                Content = userMessage 
            });
            
            var request = new OpenAIRequest
            {
                Model = "gpt-4",
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 500
            };
            
            var response = await _client.CreateChatCompletion(
                request, 
                $"Bearer {_apiKey}");
            
            return response.Choices.FirstOrDefault()?.Message.Content 
                ?? "Извините, не смог сгенерировать ответ.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            return "Извините, произошла ошибка. Попробуйте позже.";
        }
    }
}

// Services/Interfaces/IMetaMessagingService.cs
public interface IMetaMessagingService
{
    Task SendMessageAsync(string recipientId, string message, string platform);
    Task ProcessIncomingMessageAsync(MetaWebhookDto webhook);
}

// Services/Meta/MetaMessagingService.cs
public class MetaMessagingService : IMetaMessagingService
{
    private readonly IMetaApiClient _client;
    private readonly IAIService _aiService;
    private readonly IConversationContextService _contextService;
    private readonly ILogger<MetaMessagingService> _logger;
    private readonly IConfiguration _configuration;
    
    public MetaMessagingService(
        IMetaApiClient client,
        IAIService aiService,
        IConversationContextService contextService,
        ILogger<MetaMessagingService> logger,
        IConfiguration configuration)
    {
        _client = client;
        _aiService = aiService;
        _contextService = contextService;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task SendMessageAsync(string recipientId, string message, string platform)
    {
        try
        {
            var request = new MetaSendMessageRequest
            {
                Recipient = new MetaUser { Id = recipientId },
                Message = new MetaOutgoingMessage { Text = message }
            };
            
            var accessToken = platform == "whatsapp" 
                ? _configuration["Meta:WhatsApp:AccessToken"]
                : _configuration["Meta:Instagram:AccessToken"];
            
            await _client.SendMessage(request, $"Bearer {accessToken}");
            
            _logger.LogInformation(
                "Message sent to {RecipientId} on {Platform}", 
                recipientId, 
                platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {RecipientId}", recipientId);
            throw;
        }
    }
    
    public async Task ProcessIncomingMessageAsync(MetaWebhookDto webhook)
    {
        foreach (var entry in webhook.Entry)
        {
            var messagingEvents = GetMessagingEvents(entry);
            
            foreach (var messaging in messagingEvents)
            {
                if (messaging.Message?.Text == null)
                    continue;
                
                var senderId = messaging.Sender.Id;
                var messageText = messaging.Message.Text;
                var platform = DeterminePlatform(webhook.Object);
                
                // Получаем контекст беседы
                var context = await _contextService.GetContextAsync(senderId, platform);
                
                // Добавляем сообщение пользователя в контекст
                await _contextService.AddMessageAsync(
                    senderId, 
                    platform, 
                    new ChatMessage 
                    { 
                        Role = "user", 
                        Content = messageText 
                    });
                
                // Генерируем ответ через AI
                var aiResponse = await _aiService.GenerateResponseAsync(
                    messageText, 
                    context);
                
                // Сохраняем ответ в контекст
                await _contextService.AddMessageAsync(
                    senderId, 
                    platform, 
                    new ChatMessage 
                    { 
                        Role = "assistant", 
                        Content = aiResponse 
                    });
                
                // Отправляем ответ пользователю
                await SendMessageAsync(senderId, aiResponse, platform);
            }
        }
    }
    
    private List<MetaMessaging> GetMessagingEvents(MetaEntry entry)
    {
        // WhatsApp использует messaging напрямую
        if (entry.Messaging?.Any() == true)
            return entry.Messaging;
        
        // Instagram использует changes -> value -> messaging
        if (entry.Changes?.Any() == true)
        {
            return entry.Changes
                .Where(c => c.Field == "messages")
                .SelectMany(c => c.Value?.Messaging ?? new List<MetaMessaging>())
                .ToList();
        }
        
        return new List<MetaMessaging>();
    }
    
    private string DeterminePlatform(string objectType)
    {
        return objectType switch
        {
            "whatsapp_business_account" => "whatsapp",
            "instagram" => "instagram",
            _ => "unknown"
        };
    }
}