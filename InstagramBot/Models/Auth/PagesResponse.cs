using System.Text.Json.Serialization;

namespace InstagramBot.Models.Auth;

/// <summary>
/// Response from GET /me/accounts
/// Returns Facebook Pages the user is admin of
/// </summary>
public class PagesResponse
{
    [JsonPropertyName("data")]
    public List<FacebookPage> Data { get; set; } = [];
    
    [JsonPropertyName("paging")]
    public PagingInfo? Paging { get; set; }
}

public class FacebookPage
{
    /// <summary>
    /// Page ID — this is what Meta sends in webhook entry[].id
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Page-specific access token (already long-lived if parent token is long-lived)
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    /// <summary>
    /// Permissions granted for this page
    /// </summary>
    [JsonPropertyName("tasks")]
    public List<string>? Tasks { get; set; }
}

public class PagingInfo
{
    [JsonPropertyName("cursors")]
    public PagingCursors? Cursors { get; set; }
    
    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public class PagingCursors
{
    [JsonPropertyName("before")]
    public string? Before { get; set; }
    
    [JsonPropertyName("after")]
    public string? After { get; set; }
}