using System.Text.Json.Serialization;

namespace AIAgent.Models;

// Models/Meta/MetaWebhookDto.cs
public class MetaWebhookDto
{
    [JsonPropertyName("object")]
    public string Object { get; set; }
    
    [JsonPropertyName("entry")]
    public List<MetaEntry> Entry { get; set; }
}

public class MetaEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("time")]
    public long Time { get; set; }
    
    [JsonPropertyName("messaging")]
    public List<MetaMessaging> Messaging { get; set; }
    
    [JsonPropertyName("changes")]
    public List<MetaChange> Changes { get; set; } // для Instagram
}

public class MetaMessaging
{
    [JsonPropertyName("sender")]
    public MetaUser Sender { get; set; }
    
    [JsonPropertyName("recipient")]
    public MetaUser Recipient { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("message")]
    public MetaMessage Message { get; set; }
}

public class MetaChange
{
    [JsonPropertyName("value")]
    public MetaChangeValue Value { get; set; }
    
    [JsonPropertyName("field")]
    public string Field { get; set; }
}

public class MetaChangeValue
{
    [JsonPropertyName("messaging")]
    public List<MetaMessaging> Messaging { get; set; }
}

public class MetaUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class MetaMessage
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("quick_reply")]
    public MetaQuickReply QuickReply { get; set; }
}

public class MetaQuickReply
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}

// Models/Meta/MetaSendMessageRequest.cs
public class MetaSendMessageRequest
{
    [JsonPropertyName("recipient")]
    public MetaUser Recipient { get; set; }
    
    [JsonPropertyName("message")]
    public MetaOutgoingMessage Message { get; set; }
    
    [JsonPropertyName("messaging_type")]
    public string MessagingType { get; set; } = "RESPONSE";
}

public class MetaOutgoingMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

// Models/AI/ChatMessage.cs
public class ChatMessage
{
    public string Role { get; set; } // "system", "user", "assistant"
    public string Content { get; set; }
}

// Models/AI/OpenAIRequest.cs
public class OpenAIRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; }
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 500;
}

public class OpenAIResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; }
}

public class OpenAIChoice
{
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

// Models/Context/ConversationContext.cs
public class ConversationContext
{
    public string UserId { get; set; }
    public string Platform { get; set; } // "whatsapp" or "instagram"
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime LastActivity { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}