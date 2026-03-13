using InstagramBot.Data;
using InstagramBot.Models;
using InstagramBot.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstagramBot.Controllers.Auth;

[ApiController]
[Route("auth")]
public class InstagramAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<InstagramAuthController> _logger;

    public InstagramAuthController(
        IConfiguration config,
        HttpClient http,
        AppDbContext db,
        ILogger<InstagramAuthController> logger)
    {
        _config = config;
        _http = http;
        _db = db;
        _logger = logger;
    }

    // Шаг 1: Клиент нажимает "Подключить" → редирект на Facebook
    [HttpGet("instagram/connect")]
    public IActionResult Connect([FromQuery] Guid tenantId)
    {
        _logger.LogInformation("Auth connect request: tenantId={TenantId}", tenantId);

        var appId = _config["Meta:AppId"];
        var redirectUri = _config["Meta:RedirectUri"];

        var url = $"https://www.facebook.com/v25.0/dialog/oauth"
                  + $"?client_id={appId}"
                  + $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}"
                  + $"&scope=instagram_basic,instagram_manage_messages,"
                  + "pages_manage_metadata,pages_messaging,pages_show_list"
                  + $"&state={tenantId}"
                  + $"&response_type=code";

        return Redirect(url);
    }

    // Шаг 2: Facebook редиректит сюда с code
    [HttpGet("instagram/callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] Guid state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        // ── Клиент отказался от авторизации ──
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth denied: {Error} — {Description}", error, errorDescription);
            return BadRequest($"Авторизация отклонена: {errorDescription}");
        }

        if (string.IsNullOrEmpty(code))
            return BadRequest("Missing code parameter");

        _logger.LogInformation("Auth callback for tenant {TenantId}", state);

        // ── Проверяем что тенант существует ──
        var tenant = await _db.Tenants.FindAsync(state);
        if (tenant == null)
            return NotFound("Тенант не найден");

        try
        {
            var appId = _config["Meta:AppId"];
            var appSecret = _config["Meta:AppSecret"];
            var redirectUri = _config["Meta:RedirectUri"];

            // ── 1. code → short-lived token ──
            var tokenUrl = $"https://graph.facebook.com/v25.0/oauth/access_token"
                           + $"?client_id={appId}"
                           + $"&client_secret={appSecret}"
                           + $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}"
                           + $"&code={code}";

            var tokenResp = await _http.GetFromJsonAsync<TokenResponse>(tokenUrl);
            if (string.IsNullOrEmpty(tokenResp?.AccessToken))
                return StatusCode(502, "Не удалось получить токен от Facebook");

            // ── 2. short-lived → long-lived token ──
            var longLivedUrl = $"https://graph.facebook.com/v25.0/oauth/access_token"
                               + $"?grant_type=fb_exchange_token"
                               + $"&client_id={appId}"
                               + $"&client_secret={appSecret}"
                               + $"&fb_exchange_token={tokenResp.AccessToken}";

            var longLived = await _http.GetFromJsonAsync<TokenResponse>(longLivedUrl);
            if (string.IsNullOrEmpty(longLived?.AccessToken))
                return StatusCode(502, "Не удалось получить long-lived токен");

            // ── 3. Получаем список страниц клиента ──
            var pagesUrl = $"https://graph.facebook.com/v25.0/me/accounts"
                           + $"?fields=id,name,access_token"
                           + $"&access_token={longLived.AccessToken}";

            var pages = await _http.GetFromJsonAsync<PagesResponse>(pagesUrl);

            if (pages?.Data == null || pages.Data.Count == 0)
            {
                _logger.LogWarning(
                    "No pages returned for tenant {TenantId}. " +
                    "Клиент не выбрал страницу или нет прав pages_show_list", state);
                return BadRequest(
                    "Facebook не вернул ни одной страницы. " +
                    "Убедитесь, что вы выбрали свою страницу при авторизации.");
            }

            // ── 4. Создаём Channel для каждой страницы ──
            var connectedPages = new List<string>();

            foreach (var page in pages.Data)
            {
                // Проверка на дубликат
                var exists = await _db.Channels.AnyAsync(c =>
                    c.ExternalId == page.Id && c.Type == ChannelType.Instagram);

                if (exists)
                {
                    _logger.LogInformation(
                        "Page {PageId} ({PageName}) already connected, skipping",
                        page.Id, page.Name);
                    continue;
                }

                var channel = new Channel
                {
                    Id = Guid.NewGuid(),
                    TenantId = state,
                    Type = ChannelType.Instagram,
                    DisplayName = $"Instagram {page.Name}",
                    ExternalId = page.Id,
                    AccessToken = page.AccessToken, // Page-level token (permanent!)
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Channels.Add(channel);
                connectedPages.Add(page.Name);

                // ── 5. Подписываем страницу на вебхуки ──
                try
                {
                    await _http.PostAsync(
                        $"https://graph.facebook.com/v25.0/{page.Id}/subscribed_apps"
                        + $"?subscribed_fields=messages,messaging_postbacks"
                        + $"&access_token={page.AccessToken}", null);

                    _logger.LogInformation(
                        "Subscribed page {PageId} ({PageName}) to webhooks",
                        page.Id, page.Name);
                }
                catch (Exception ex)
                {
                    // Не критично — можно подписать вручную позже
                    _logger.LogWarning(ex,
                        "Failed to subscribe page {PageId} to webhooks", page.Id);
                }
            }

            await _db.SaveChangesAsync();

            if (connectedPages.Count == 0)
                return Ok("Все страницы уже были подключены ранее.");

            _logger.LogInformation(
                "Connected {Count} page(s) for tenant {TenantId}: {Pages}",
                connectedPages.Count, state, string.Join(", ", connectedPages));

            // В продакшене — редирект на фронтенд:
            // return Redirect($"https://yourdomain.com/dashboard?connected=true");

            return Ok(new
            {
                success = true,
                message = $"Подключено страниц: {connectedPages.Count}",
                pages = connectedPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for tenant {TenantId}", state);
            return StatusCode(500, "Ошибка при подключении. Попробуйте ещё раз.");
        }
    }
}