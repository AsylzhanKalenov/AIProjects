using System.Text.Json.Serialization;

namespace InstagramBot.Models.WhatsApp;

// ============================================================
// SEND MESSAGE REQUESTS
// ============================================================

/// <summary>
/// Base request to send any type of WhatsApp message
/// Docs: https://developers.facebook.com/docs/whatsapp/cloud-api/messages
/// </summary>
public class WhatsAppSendRequest
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = "whatsapp";
    
    [JsonPropertyName("recipient_type")]
    public string RecipientType { get; set; } = "individual";
    
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty; // Phone number with country code
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // text, image, document, audio, video, template, interactive, location
    
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendText? Text { get; set; }
    
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendMedia? Image { get; set; }
    
    [JsonPropertyName("document")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendDocument? Document { get; set; }
    
    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendMedia? Audio { get; set; }
    
    [JsonPropertyName("video")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendMedia? Video { get; set; }
    
    [JsonPropertyName("template")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendTemplate? Template { get; set; }
    
    [JsonPropertyName("interactive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendInteractive? Interactive { get; set; }
    
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendLocation? Location { get; set; }
    
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppReplyContext? Context { get; set; }
}

/// <summary>
/// Reply to a specific message
/// </summary>
public class WhatsAppReplyContext
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
}

// ---- Text ----
public class WhatsAppSendText
{
    [JsonPropertyName("preview_url")]
    public bool PreviewUrl { get; set; } = false;
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

// ---- Media ----
public class WhatsAppSendMedia
{
    /// <summary>
    /// Media ID (if uploaded to WhatsApp) — use either Id or Link
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
    
    /// <summary>
    /// Public URL of the media — use either Id or Link
    /// </summary>
    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
    
    [JsonPropertyName("caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; set; }
}

public class WhatsAppSendDocument : WhatsAppSendMedia
{
    [JsonPropertyName("filename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filename { get; set; }
}

// ---- Location ----
public class WhatsAppSendLocation
{
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }
}

// ---- Template Messages ----
public class WhatsAppSendTemplate
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("language")]
    public WhatsAppTemplateLanguage Language { get; set; } = new();
    
    [JsonPropertyName("components")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppTemplateComponent>? Components { get; set; }
}

public class WhatsAppTemplateLanguage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "ru"; // Language code
}

public class WhatsAppTemplateComponent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "header", "body", "button"
    
    [JsonPropertyName("sub_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SubType { get; set; } // "quick_reply", "url"
    
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; set; } // For buttons
    
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppTemplateParameter>? Parameters { get; set; }
}

public class WhatsAppTemplateParameter
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // "text", "currency", "date_time", "image", "document", "video"
    
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
    
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendMedia? Image { get; set; }
    
    [JsonPropertyName("document")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendDocument? Document { get; set; }
}

// ---- Interactive Messages (Buttons & Lists) ----
public class WhatsAppSendInteractive
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "button", "list", "product", "product_list"
    
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppInteractiveHeader? Header { get; set; }
    
    [JsonPropertyName("body")]
    public WhatsAppInteractiveBody Body { get; set; } = new();
    
    [JsonPropertyName("footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppInteractiveFooter? Footer { get; set; }
    
    [JsonPropertyName("action")]
    public WhatsAppInteractiveAction Action { get; set; } = new();
}

public class WhatsAppInteractiveHeader
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // "text", "image", "video", "document"
    
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
    
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppSendMedia? Image { get; set; }
}

public class WhatsAppInteractiveBody
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class WhatsAppInteractiveFooter
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class WhatsAppInteractiveAction
{
    /// <summary>
    /// For "button" type: up to 3 reply buttons
    /// </summary>
    [JsonPropertyName("buttons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppInteractiveButton>? Buttons { get; set; }
    
    /// <summary>
    /// For "list" type: button text that opens the list
    /// </summary>
    [JsonPropertyName("button")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Button { get; set; }
    
    /// <summary>
    /// For "list" type: sections with rows
    /// </summary>
    [JsonPropertyName("sections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppInteractiveSection>? Sections { get; set; }
}

public class WhatsAppInteractiveButton
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "reply";
    
    [JsonPropertyName("reply")]
    public WhatsAppButtonContent Reply { get; set; } = new();
}

public class WhatsAppButtonContent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Max 256 chars
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty; // Max 20 chars
}

public class WhatsAppInteractiveSection
{
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; } // Max 24 chars
    
    [JsonPropertyName("rows")]
    public List<WhatsAppSectionRow> Rows { get; set; } = [];
}

public class WhatsAppSectionRow
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Max 200 chars
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty; // Max 24 chars
    
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; } // Max 72 chars
}

// ============================================================
// API RESPONSES
// ============================================================

public class WhatsAppSendResponse
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }
    
    [JsonPropertyName("contacts")]
    public List<WhatsAppResponseContact>? Contacts { get; set; }
    
    [JsonPropertyName("messages")]
    public List<WhatsAppResponseMessage>? Messages { get; set; }
    
    [JsonPropertyName("error")]
    public WhatsAppApiError? Error { get; set; }
}

public class WhatsAppResponseContact
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }
    
    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }
}

public class WhatsAppResponseMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // wamid.xxx
    
    [JsonPropertyName("message_status")]
    public string? MessageStatus { get; set; }
}

public class WhatsAppApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }
    
    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}

// ============================================================
// MEDIA RETRIEVAL
// ============================================================

public class WhatsAppMediaUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }
    
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
    
    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }
}
