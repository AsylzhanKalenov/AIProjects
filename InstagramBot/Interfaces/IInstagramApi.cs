using InstagramBot.Models;
using Refit;

namespace InstagramBot.Interfaces;

/// <summary>
/// Instagram Graph API client (via Refit)
/// </summary>
public interface IInstagramApi
{
    /// <summary>
    /// Send a message to a user
    /// </summary>
    [Post("/v21.0/me/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        [Query] string access_token,
        [Body] SendMessageRequest request);

    /// <summary>
    /// Get user profile info
    /// </summary>
    [Get("/v21.0/{userId}")]
    Task<UserProfile> GetUserProfileAsync(
        string userId,
        [Query] string fields,
        [Query] string access_token);
}

public class UserProfile
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ProfilePic { get; set; }
}
