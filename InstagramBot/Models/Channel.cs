namespace InstagramBot.Models;

/// <summary>
/// Supported messaging channels
/// </summary>
public enum ChannelType
{
    Instagram = 0,
    WhatsApp = 1
    // Telegram = 2,  // Future
    // FacebookMessenger = 3  // Future
}

/// <summary>
/// Represents a messaging channel connected to a tenant.
/// One tenant can have multiple channels (Instagram + WhatsApp + ...)
/// </summary>
public class Channel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public ChannelType Type { get; set; }
    
    /// <summary>
    /// Display name, e.g. "Instagram @shopname" or "WhatsApp +7 777 ..."
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Channel-specific external ID:
    /// - Instagram: Instagram Business Account ID (Page ID)
    /// - WhatsApp: Phone Number ID
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Access token for the channel API
    /// - Instagram: Page Access Token
    /// - WhatsApp: Permanent/System User Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// WhatsApp Business Account ID (only for WhatsApp channels)
    /// </summary>
    public string? WhatsAppBusinessAccountId { get; set; }
    
    /// <summary>
    /// Phone number in international format (only for WhatsApp)
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Conversation> Conversations { get; set; } = [];
}
