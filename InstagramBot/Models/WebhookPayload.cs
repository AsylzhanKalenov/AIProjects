using System.Text.Json.Serialization;

namespace InstagramBot.Models;

/// <summary>
/// Incoming webhook payload from Meta/Instagram
/// </summary>
public class WebhookPayload
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("entry")]
    public List<Entry> Entry { get; set; } = [];
}

public class Entry
{
    /// <summary>
    /// Instagram Business Account ID (matches Tenant.InstagramPageId)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("time")]
    public long Time { get; set; }
    
    [JsonPropertyName("messaging")]
    public List<MessagingEvent>? Messaging { get; set; }
}

public class MessagingEvent
{
    [JsonPropertyName("sender")]
    public Participant Sender { get; set; } = new();
    
    [JsonPropertyName("recipient")]
    public Participant Recipient { get; set; } = new();
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("message")]
    public IncomingMessage? Message { get; set; }
    
    [JsonPropertyName("postback")]
    public Postback? Postback { get; set; }
}

public class Participant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class IncomingMessage
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }
}

public class Attachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public AttachmentPayload? Payload { get; set; }
}

public class AttachmentPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class Postback
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}
