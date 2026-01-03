using System.Text.Json.Serialization;

namespace InstagramBot.Models;

/// <summary>
/// Request to send a message via Instagram API
/// </summary>
public class SendMessageRequest
{
    [JsonPropertyName("recipient")]
    public MessageRecipient Recipient { get; set; } = new();
    
    [JsonPropertyName("message")]
    public OutgoingMessage Message { get; set; } = new();
    
    [JsonPropertyName("messaging_type")]
    public string MessagingType { get; set; } = "RESPONSE";
}

public class MessageRecipient
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class OutgoingMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("attachment")]
    public OutgoingAttachment? Attachment { get; set; }
    
    [JsonPropertyName("quick_replies")]
    public List<QuickReply>? QuickReplies { get; set; }
}

public class OutgoingAttachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "template";
    
    [JsonPropertyName("payload")]
    public TemplatePayload Payload { get; set; } = new();
}

public class TemplatePayload
{
    [JsonPropertyName("template_type")]
    public string TemplateType { get; set; } = "button";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("buttons")]
    public List<Button>? Buttons { get; set; }
}

public class Button
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "postback";
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

public class QuickReply
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "text";
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

/// <summary>
/// Response from Instagram API after sending a message
/// </summary>
public class SendMessageResponse
{
    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; set; }
    
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
    
    [JsonPropertyName("error")]
    public MetaApiError? Error { get; set; }
}

public class MetaApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}
