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
    
    /// <summary>
    /// Read receipt: sent when a message has been read by the recipient.
    /// Contains "mid" of the last read message.
    /// </summary>
    [JsonPropertyName("read")]
    public ReadReceipt? Read { get; set; }
    
    /// <summary>
    /// Reaction event: sent when a customer reacts/unreacts to a message
    /// </summary>
    [JsonPropertyName("reaction")]
    public MessageReaction? Reaction { get; set; }
    
    /// <summary>
    /// Referral data: sent when a customer clicks an ig.me link
    /// with a referral parameter in an existing conversation
    /// </summary>
    [JsonPropertyName("referral")]
    public Referral? Referral { get; set; }
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
    
    /// <summary>
    /// Present and true when this message is an echo of a message
    /// sent BY the business account (i.e. our own outgoing message).
    /// MUST be filtered out to prevent the bot from replying to itself.
    /// </summary>
    [JsonPropertyName("is_echo")]
    public bool? IsEcho { get; set; }
    
    /// <summary>
    /// Present and true when a customer deletes a message
    /// </summary>
    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }
    
    /// <summary>
    /// Present and true when the customer sends unsupported media
    /// (e.g. disappearing media, certain sticker types)
    /// </summary>
    [JsonPropertyName("is_unsupported")]
    public bool? IsUnsupported { get; set; }
    
    /// <summary>
    /// Present when the customer selects a quick reply button.
    /// Contains the payload string set on the quick reply option.
    /// </summary>
    [JsonPropertyName("quick_reply")]
    public QuickReplyResponse? QuickReply { get; set; }
    
    /// <summary>
    /// Present when the message is an inline reply to another message
    /// or a reply to a story.
    /// </summary>
    [JsonPropertyName("reply_to")]
    public ReplyTo? ReplyTo { get; set; }
    
    /// <summary>
    /// Present when a customer clicks an Instagram Shop product
    /// or a Click-To-Direct (CTD) ad
    /// </summary>
    [JsonPropertyName("referral")]
    public MessageReferral? Referral { get; set; }
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

// ---- Additional webhook event models ----

public class ReadReceipt
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;
}

public class MessageReaction
{
    /// <summary>
    /// ID of the message that was reacted to
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;
    
    /// <summary>
    /// "react" or "unreact"
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Reaction type: love, like, laugh, wow, sad, angry, other.
    /// Not present on "unreact".
    /// </summary>
    [JsonPropertyName("reaction")]
    public string? Reaction { get; set; }
    
    /// <summary>
    /// Emoji character. Not present on "unreact".
    /// </summary>
    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }
}

public class Referral
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
    
    [JsonPropertyName("source")]
    public string? Source { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class QuickReplyResponse
{
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

public class ReplyTo
{
    /// <summary>
    /// Message ID of the message being replied to (inline reply)
    /// </summary>
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }
    
    /// <summary>
    /// Present when replying to a story
    /// </summary>
    [JsonPropertyName("story")]
    public StoryReply? Story { get; set; }
}

public class StoryReply
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Referral inside a message (Instagram Shop / CTD ad)
/// </summary>
public class MessageReferral
{
    /// <summary>
    /// Product info when clicking from Instagram Shops
    /// </summary>
    [JsonPropertyName("product")]
    public ReferralProduct? Product { get; set; }
    
    /// <summary>
    /// Ref data from the ad (if specified)
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
    
    [JsonPropertyName("ad_id")]
    public string? AdId { get; set; }
    
    [JsonPropertyName("source")]
    public string? Source { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("ads_context_data")]
    public AdsContextData? AdsContextData { get; set; }
}

public class ReferralProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class AdsContextData
{
    [JsonPropertyName("ad_title")]
    public string? AdTitle { get; set; }
    
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; set; }
    
    [JsonPropertyName("video_url")]
    public string? VideoUrl { get; set; }
}