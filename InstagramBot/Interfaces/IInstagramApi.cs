using InstagramBot.Models;
using Refit;

namespace InstagramBot.Interfaces;

/// <summary>
/// Instagram API client via Instagram Login (graph.instagram.com).
/// 
/// Key differences from old Facebook Login approach:
///   - Base URL: graph.instagram.com (not graph.facebook.com)
///   - Auth: Bearer token in Authorization header (not query param)
///   - Endpoint: POST /{ig_user_id}/messages (not /me/messages)
/// </summary>
public interface IInstagramApi
{
    /// <summary>
    /// Send a message to an Instagram user.
    /// POST /{ig_user_id}/messages
    /// 
    /// Uses Authorization: Bearer header for authentication.
    /// </summary>
    [Post("/v25.0/{igUserId}/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        string igUserId,
        [Header("Authorization")] string bearerToken,
        [Body] SendMessageRequest request);

    /// <summary>
    /// Get user profile info
    /// </summary>
    [Get("/v25.0/{userId}")]
    Task<UserProfile> GetUserProfileAsync(
        string userId,
        [Query] string fields,
        [Header("Authorization")] string bearerToken);
}

public class UserProfile
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? ProfilePic { get; set; }
}