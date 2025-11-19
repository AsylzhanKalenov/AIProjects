using AIAgent.Interfaces;
using AIAgent.Models;

namespace AIAgent.Services;

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