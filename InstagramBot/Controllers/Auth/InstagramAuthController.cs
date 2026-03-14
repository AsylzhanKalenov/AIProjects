using InstagramBot.Data;
using InstagramBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstagramBot.Controllers.Auth;

/// <summary>
/// OAuth flow using Instagram Business Login (instagram.com/oauth/authorize).
/// 
/// This is the NEW approach — client logs in via Instagram directly,
/// no Facebook Page needed.
/// 
/// Flow:
///   1. GET /auth/instagram/connect?tenantId=xxx → redirect to Instagram
///   2. Client logs into Instagram, grants permissions
///   3. Instagram redirects to GET /auth/instagram/callback?code=xxx&state=tenantId
///   4. Server exchanges code → short-lived token → long-lived token
///   5. Gets IG User ID → creates Channel → done!
///
/// Key differences from Facebook Login:
///   - OAuth URL: instagram.com (not facebook.com)
///   - Token exchange: POST form-data to api.instagram.com
///   - Long-lived exchange: GET graph.instagram.com/access_token
///   - Tokens expire in 60 days (need refresh!)
///   - Uses Instagram App ID/Secret (not Facebook App ID)
///   - No Facebook Page required
/// </summary>
[ApiController]
[Route("auth")]
public class InstagramAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<InstagramAuthController> _logger;

    public InstagramAuthController(
        AppDbContext db,
        HttpClient http,
        IConfiguration config,
        ILogger<InstagramAuthController> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Redirect client to Instagram OAuth
    /// Usage: GET /auth/instagram/connect?tenantId=xxx
    /// </summary>
    [HttpGet("instagram/connect")]
    public IActionResult Connect([FromQuery] Guid tenantId)
    {
        _logger.LogInformation("Auth connect request: tenantId={TenantId}", tenantId);

        var appId = _config["Instagram:AppId"];
        var redirectUri = _config["Instagram:RedirectUri"];

        // Instagram Business Login URL
        var url = $"https://www.instagram.com/oauth/authorize"
                  + $"?client_id={appId}"
                  + $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}"
                  + $"&scope=instagram_business_basic,instagram_business_manage_messages"
                  + $"&response_type=code"
                  + $"&state={tenantId}";

        return Redirect(url);
    }

    /// <summary>
    /// Step 2: Instagram redirects here with authorization code
    /// </summary>
    [HttpGet("instagram/callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] Guid state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        // Client denied authorization
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth denied: {Error} — {Description}", error, errorDescription);
            return BadRequest($"Авторизация отклонена: {errorDescription}");
        }

        if (string.IsNullOrEmpty(code))
            return BadRequest("Missing code parameter");

        // Strip #_ suffix that Instagram appends
        code = code.TrimEnd('#', '_');

        _logger.LogInformation("Auth callback for tenant {TenantId}", state);

        var tenant = await _db.Tenants.FindAsync(state);
        if (tenant == null)
            return NotFound("Тенант не найден");

        try
        {
            var appId = _config["Instagram:AppId"];
            var appSecret = _config["Instagram:AppSecret"];
            var redirectUri = _config["Instagram:RedirectUri"];
            
            _logger.LogInformation(
                "Token exchange params: client_id={AppId}, redirect_uri={RedirectUri}, code_length={CodeLength}",
                appId, redirectUri, code?.Length);

            // ── 1. Exchange code → short-lived token (POST form-data) ──
            var tokenResponse = await _http.PostAsync(
                "https://api.instagram.com/oauth/access_token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = appId!,
                    ["client_secret"] = appSecret!,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = redirectUri!,
                    ["code"] = code
                }));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {Response}", tokenJson);
                return StatusCode(502, "Не удалось получить токен от Instagram");
            }

            var tokenResult = JsonSerializer.Deserialize<InstagramTokenResponse>(tokenJson);
            if (tokenResult?.Data == null || tokenResult.Data.Count == 0)
            {
                _logger.LogError("Empty token response: {Response}", tokenJson);
                return StatusCode(502, "Пустой ответ при обмене токена");
            }

            var shortLivedToken = tokenResult.Data[0].AccessToken;
            var igUserId = tokenResult.Data[0].UserId;

            _logger.LogInformation("Got short-lived token for IG user {IgUserId}", igUserId);

            // ── 2. Exchange short-lived → long-lived token (60 days) ──
            var longLivedUrl = $"https://graph.instagram.com/access_token"
                               + $"?grant_type=ig_exchange_token"
                               + $"&client_secret={appSecret}"
                               + $"&access_token={shortLivedToken}";

            var longLivedResp = await _http.GetAsync(longLivedUrl);
            var longLivedJson = await longLivedResp.Content.ReadAsStringAsync();

            if (!longLivedResp.IsSuccessStatusCode)
            {
                _logger.LogError("Long-lived token exchange failed: {Response}", longLivedJson);
                return StatusCode(502, "Не удалось получить long-lived токен");
            }

            var longLived = JsonSerializer.Deserialize<LongLivedTokenResponse>(longLivedJson);
            var expiresAt = DateTime.UtcNow.AddSeconds(longLived?.ExpiresIn ?? 5184000);

            // ── 3. Get Instagram account info ──
            var profileUrl = $"https://graph.instagram.com/v25.0/me"
                             + $"?fields=user_id,username,name"
                             + $"&access_token={longLived!.AccessToken}";

            var profileResp = await _http.GetAsync(profileUrl);
            var profileJson = await profileResp.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<InstagramProfile>(profileJson);

            var displayName = profile?.Username ?? profile?.Name ?? igUserId;

            // ── 4. Check for duplicate ──
            var exists = await _db.Channels.AnyAsync(c =>
                c.ExternalId == igUserId && c.Type == ChannelType.Instagram);

            if (exists)
            {
                // Update token if channel already exists
                var existingChannel = await _db.Channels.FirstAsync(c =>
                    c.ExternalId == igUserId && c.Type == ChannelType.Instagram);
                existingChannel.AccessToken = longLived.AccessToken;
                existingChannel.TokenExpiresAt = expiresAt;
                existingChannel.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated token for existing channel {IgUserId}", igUserId);
                return Ok(new { success = true, message = "Токен обновлён", account = displayName });
            }

            // ── 5. Create Channel ──
            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                TenantId = state,
                Type = ChannelType.Instagram,
                DisplayName = $"Instagram @{displayName}",
                ExternalId = igUserId,       // Instagram-scoped User ID
                AccessToken = longLived.AccessToken,
                TokenExpiresAt = expiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Channels.Add(channel);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Created Instagram channel for @{Username} (IG ID: {IgUserId}), tenant {TenantId}",
                displayName, igUserId, state);

            // В продакшене — редирект на фронтенд:
            // return Redirect($"https://yourdomain.com/dashboard?connected=true");

            return Ok(new
            {
                success = true,
                message = "Instagram подключён!",
                account = displayName,
                igUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for tenant {TenantId}", state);
            return StatusCode(500, "Ошибка при подключении. Попробуйте ещё раз.");
        }
    }

    // ================================================================
    // DTOs for Instagram API responses
    // ================================================================

    private class InstagramTokenResponse
    {
        [JsonPropertyName("data")]
        public List<InstagramTokenData>? Data { get; set; }
    }

    private class InstagramTokenData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("permissions")]
        public string? Permissions { get; set; }
    }

    private class LongLivedTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public long? ExpiresIn { get; set; }
    }

    private class InstagramProfile
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}