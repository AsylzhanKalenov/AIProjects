namespace InstagramBot.Models;

/// <summary>
/// Represents a business client (tenant) using the chatbot platform
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Business name for display
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;
    
    /// <summary>
    /// Instagram Business Account ID (received from Meta)
    /// </summary>
    public string InstagramPageId { get; set; } = string.Empty;
    
    /// <summary>
    /// Long-lived Page Access Token from Meta
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// System prompt for AI (describes bot personality and role)
    /// Example: "Ты дружелюбный консультант магазина обуви..."
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Knowledge base content (FAQ, products, prices, etc.)
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
    /// Monthly message limit
    /// </summary>
    public int MonthlyMessageLimit { get; set; } = 1000;
    
    /// <summary>
    /// Current month message count
    /// </summary>
    public int CurrentMonthMessages { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Conversation> Conversations { get; set; } = [];
}

/// <summary>
/// Represents a conversation with a user
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Instagram User ID (sender)
    /// </summary>
    public string InstagramUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User's name if available
    /// </summary>
    public string? UserName { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Tenant Tenant { get; set; } = null!;
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
    /// Instagram message ID
    /// </summary>
    public string? InstagramMessageId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Conversation Conversation { get; set; } = null!;
}
