using InstagramBot.Data;
using InstagramBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstagramBot.Services;

/// <summary>
/// Background service that refreshes Instagram long-lived tokens before they expire.
/// 
/// Instagram tokens expire in 60 days. This service checks daily and refreshes
/// tokens that expire within 7 days.
/// 
/// Refresh endpoint: GET https://graph.instagram.com/refresh_access_token
///   ?grant_type=ig_refresh_token
///   &access_token={LONG_LIVED_TOKEN}
/// 
/// Requirements:
///   - Token must be at least 24 hours old
///   - Token must not be expired
///   - User must have granted instagram_business_basic
/// </summary>
public class TokenRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenRefreshService> _logger;

    // Refresh tokens that expire within 7 days
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromDays(7);

    // Check once per day
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public TokenRefreshService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token refresh service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExpiringTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RefreshExpiringTokensAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var threshold = DateTime.UtcNow.Add(RefreshThreshold);

        // Find Instagram channels with tokens expiring soon
        var channels = await db.Channels
            .Where(c =>
                c.Type == ChannelType.Instagram &&
                c.IsActive &&
                c.TokenExpiresAt != null &&
                c.TokenExpiresAt < threshold &&
                c.TokenExpiresAt > DateTime.UtcNow) // Not yet expired
            .ToListAsync(ct);

        if (channels.Count == 0)
        {
            _logger.LogDebug("No Instagram tokens need refreshing");
            return;
        }

        _logger.LogInformation("Found {Count} Instagram token(s) to refresh", channels.Count);

        var http = _httpClientFactory.CreateClient();

        foreach (var channel in channels)
        {
            try
            {
                var url = $"https://graph.instagram.com/refresh_access_token"
                          + $"?grant_type=ig_refresh_token"
                          + $"&access_token={channel.AccessToken}";

                var response = await http.GetAsync(url, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Failed to refresh token for channel {ChannelId}: {Response}",
                        channel.Id, json);
                    continue;
                }

                var result = JsonSerializer.Deserialize<RefreshTokenResponse>(json);

                if (string.IsNullOrEmpty(result?.AccessToken))
                {
                    _logger.LogError("Empty token in refresh response for channel {ChannelId}", channel.Id);
                    continue;
                }

                channel.AccessToken = result.AccessToken;
                channel.TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn ?? 5184000);
                channel.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Refreshed token for channel {ChannelId} ({DisplayName}), new expiry: {ExpiresAt}",
                    channel.Id, channel.DisplayName, channel.TokenExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error refreshing token for channel {ChannelId}", channel.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private class RefreshTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public long? ExpiresIn { get; set; }
    }
}