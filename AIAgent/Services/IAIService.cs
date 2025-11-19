using AIAgent.Interfaces;
using AIAgent.Models;

namespace AIAgent.Services;

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