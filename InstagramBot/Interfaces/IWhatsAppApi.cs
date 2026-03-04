using InstagramBot.Models.WhatsApp;
using Refit;

namespace InstagramBot.Interfaces;

/// <summary>
/// WhatsApp Cloud API client (via Refit)
/// Docs: https://developers.facebook.com/docs/whatsapp/cloud-api
/// </summary>
public interface IWhatsAppApi
{
    /// <summary>
    /// Send a message (text, media, template, interactive)
    /// POST /{phone_number_id}/messages
    /// </summary>
    [Post("/v21.0/{phoneNumberId}/messages")]
    Task<WhatsAppSendResponse> SendMessageAsync(
        string phoneNumberId,
        [Query] string access_token,
        [Body] WhatsAppSendRequest request);

    /// <summary>
    /// Get media URL by media ID (to download user-sent media)
    /// GET /{media_id}
    /// </summary>
    [Get("/v21.0/{mediaId}")]
    Task<WhatsAppMediaUrlResponse> GetMediaUrlAsync(
        string mediaId,
        [Query] string access_token);

    /// <summary>
    /// Mark a message as read
    /// POST /{phone_number_id}/messages
    /// </summary>
    [Post("/v21.0/{phoneNumberId}/messages")]
    Task MarkAsReadAsync(
        string phoneNumberId,
        [Query] string access_token,
        [Body] WhatsAppMarkReadRequest request);
}

/// <summary>
/// Request to mark a message as read (blue ticks)
/// </summary>
public class WhatsAppMarkReadRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = "whatsapp";
    
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = "read";
    
    [System.Text.Json.Serialization.JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
}
