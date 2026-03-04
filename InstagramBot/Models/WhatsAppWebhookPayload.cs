using System.Text.Json.Serialization;

namespace InstagramBot.Models.WhatsApp;

/// <summary>
/// Root webhook payload from WhatsApp Cloud API
/// Docs: https://developers.facebook.com/docs/whatsapp/cloud-api/webhooks/payload-examples
/// </summary>
public class WhatsAppWebhookPayload
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("entry")]
    public List<WhatsAppEntry> Entry { get; set; } = [];
}

public class WhatsAppEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // WABA ID
    
    [JsonPropertyName("changes")]
    public List<WhatsAppChange> Changes { get; set; } = [];
}

public class WhatsAppChange
{
    [JsonPropertyName("value")]
    public WhatsAppValue Value { get; set; } = new();
    
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty; // "messages"
}

public class WhatsAppValue
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = string.Empty; // "whatsapp"
    
    [JsonPropertyName("metadata")]
    public WhatsAppMetadata Metadata { get; set; } = new();
    
    [JsonPropertyName("contacts")]
    public List<WhatsAppContact>? Contacts { get; set; }
    
    [JsonPropertyName("messages")]
    public List<WhatsAppMessage>? Messages { get; set; }
    
    [JsonPropertyName("statuses")]
    public List<WhatsAppStatus>? Statuses { get; set; }
    
    [JsonPropertyName("errors")]
    public List<WhatsAppError>? Errors { get; set; }
}

public class WhatsAppMetadata
{
    /// <summary>
    /// Phone number ID that received the message (used to find Channel)
    /// </summary>
    [JsonPropertyName("display_phone_number")]
    public string DisplayPhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("phone_number_id")]
    public string PhoneNumberId { get; set; } = string.Empty;
}

public class WhatsAppContact
{
    [JsonPropertyName("profile")]
    public WhatsAppProfile Profile { get; set; } = new();
    
    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty; // Phone number
}

public class WhatsAppProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class WhatsAppMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty; // Sender phone number
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Message ID (wamid.xxx)
    
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // text, image, document, audio, video, sticker, location, contacts, interactive, button
    
    [JsonPropertyName("text")]
    public WhatsAppTextContent? Text { get; set; }
    
    [JsonPropertyName("image")]
    public WhatsAppMediaContent? Image { get; set; }
    
    [JsonPropertyName("document")]
    public WhatsAppMediaContent? Document { get; set; }
    
    [JsonPropertyName("audio")]
    public WhatsAppMediaContent? Audio { get; set; }
    
    [JsonPropertyName("video")]
    public WhatsAppMediaContent? Video { get; set; }
    
    [JsonPropertyName("sticker")]
    public WhatsAppMediaContent? Sticker { get; set; }
    
    [JsonPropertyName("location")]
    public WhatsAppLocation? Location { get; set; }
    
    [JsonPropertyName("interactive")]
    public WhatsAppInteractiveReply? Interactive { get; set; }
    
    [JsonPropertyName("button")]
    public WhatsAppButtonReply? Button { get; set; }
    
    [JsonPropertyName("context")]
    public WhatsAppContext? Context { get; set; }
}

public class WhatsAppTextContent
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppMediaContent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Media ID
    
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }
    
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
    
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
    
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
}

public class WhatsAppLocation
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

/// <summary>
/// Reply from interactive message (list or button)
/// </summary>
public class WhatsAppInteractiveReply
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "button_reply" or "list_reply"
    
    [JsonPropertyName("button_reply")]
    public WhatsAppReplyItem? ButtonReply { get; set; }
    
    [JsonPropertyName("list_reply")]
    public WhatsAppReplyItem? ListReply { get; set; }
}

public class WhatsAppReplyItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Reply from template button
/// </summary>
public class WhatsAppButtonReply
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

/// <summary>
/// Context of a replied-to message
/// </summary>
public class WhatsAppContext
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Original message ID
}

/// <summary>
/// Message delivery status update
/// </summary>
public class WhatsAppStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // sent, delivered, read, failed
    
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;
    
    [JsonPropertyName("errors")]
    public List<WhatsAppError>? Errors { get; set; }
}

public class WhatsAppError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("error_data")]
    public WhatsAppErrorData? ErrorData { get; set; }
}

public class WhatsAppErrorData
{
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
