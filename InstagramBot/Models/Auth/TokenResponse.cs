using System.Text.Json.Serialization;

namespace InstagramBot.Models.Auth;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
    
    /// <summary>
    /// Seconds until expiration (long-lived ≈ 5184000 = 60 days)
    /// </summary>
    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; set; }
}