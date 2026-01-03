using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstagramBot.Services;

public interface IOpenAiService
{
    Task<string> GetResponseAsync(string systemPrompt, string knowledgeBase, string userMessage, List<ChatMessage>? history = null);
}

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiService(HttpClient http, IConfiguration config, ILogger<OpenAiService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentException("OpenAI:ApiKey not configured");
        _model = config["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<string> GetResponseAsync(
        string systemPrompt, 
        string knowledgeBase, 
        string userMessage,
        List<ChatMessage>? history = null)
    {
        try
        {
            var fullSystemPrompt = $@"{systemPrompt}

=== ИНФОРМАЦИЯ О КОМПАНИИ ===
{knowledgeBase}
=== КОНЕЦ ИНФОРМАЦИИ ===

Отвечай на основе предоставленной информации. Если информации недостаточно - честно скажи об этом.
Отвечай кратко, не более 2-3 предложений, если вопрос простой.";

            var messages = new List<object>
            {
                new { role = "system", content = fullSystemPrompt }
            };

            // Add conversation history if provided
            if (history != null)
            {
                foreach (var msg in history.TakeLast(10)) // Last 10 messages for context
                {
                    messages.Add(new 
                    { 
                        role = msg.IsFromUser ? "user" : "assistant", 
                        content = msg.Content 
                    });
                }
            }

            // Add current message
            messages.Add(new { role = "user", content = userMessage });

            var request = new
            {
                model = _model,
                messages = messages,
                max_tokens = 500,
                temperature = 0.7
            };

            _http.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions", 
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {Error}", error);
                return "Извините, произошла техническая ошибка. Попробуйте позже.";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            
            return result?.Choices?.FirstOrDefault()?.Message?.Content 
                ?? "Извините, не удалось получить ответ.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return "Извините, произошла ошибка. Попробуйте позже.";
        }
    }
}

// DTOs for OpenAI API
public class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// For conversation history
public class ChatMessage
{
    public bool IsFromUser { get; set; }
    public string Content { get; set; } = string.Empty;
}
