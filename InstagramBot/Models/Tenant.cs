namespace InstagramBot.Models;

/// <summary>
/// Represents a business client (tenant) using the chatbot platform.
/// A tenant can have multiple channels (Instagram, WhatsApp, etc.)
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Business name for display
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;
    
    /// <summary>
    /// [DEPRECATED - use Channel.ExternalId] Instagram Business Account ID
    /// Kept for backward compatibility during migration
    /// </summary>
    public string InstagramPageId { get; set; } = string.Empty;
    
    /// <summary>
    /// [DEPRECATED - use Channel.AccessToken] Long-lived Page Access Token
    /// Kept for backward compatibility during migration
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// System prompt for AI (describes bot personality and role)
    /// Shared across all channels for this tenant
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Knowledge base content (FAQ, products, prices, etc.)
    /// Shared across all channels for this tenant
    /// </summary>
    public string KnowledgeBase { get; set; } = string.Empty;
    
    /// <summary>
    /// Greeting message for new conversations
    /// </summary>
    public string? WelcomeMessage { get; set; }
    
    /// <summary>
    /// Message when bot doesn't understand
    /// </summary>
    public string? FallbackMessage { get; set; }
    
    /// <summary>
    /// Whether this tenant is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Monthly message limit (across all channels)
    /// </summary>
    public int MonthlyMessageLimit { get; set; } = 1000;
    
    /// <summary>
    /// Current month message count (across all channels)
    /// </summary>
    public int CurrentMonthMessages { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Channel> Channels { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}

/// <summary>
/// Represents a conversation with a user on a specific channel
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Channel this conversation belongs to (nullable for backward compat)
    /// </summary>
    public Guid? ChannelId { get; set; }
    
    /// <summary>
    /// External user identifier:
    /// - Instagram: Instagram User ID (IGSID)
    /// - WhatsApp: Phone number (e.g. "77771234567")
    /// </summary>
    public string InstagramUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User's display name if available
    /// </summary>
    public string? UserName { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Channel? Channel { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
}

/// <summary>
/// Individual message in a conversation
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// True if message is from user, false if from bot
    /// </summary>
    public bool IsFromUser { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// External message ID:
    /// - Instagram: mid.xxx
    /// - WhatsApp: wamid.xxx
    /// </summary>
    public string? InstagramMessageId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Conversation Conversation { get; set; } = null!;
}
