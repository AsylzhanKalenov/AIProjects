using InstagramBot.Data;
using InstagramBot.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace InstagramBot.Controllers.Auth;

[ApiController]
[Route("auth")]
public class InstagramAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<InstagramAuthController> _logger;

    public InstagramAuthController(IConfiguration config, HttpClient http, AppDbContext db, ILogger<InstagramAuthController> logger)
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
        // например: https://mybot.kz/auth/instagram/callback

        var url = $"https://www.facebook.com/v25.0/dialog/oauth"
                  + $"?client_id={appId}"
                  + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                  + $"&scope=instagram_basic,instagram_manage_messages,"
                  + "pages_manage_metadata,pages_read_engagement,pages_show_list"
                  + $"&state={tenantId}"
                  + $"&response_type=code";

        return Redirect(url);
    }

    // Шаг 2: Facebook редиректит сюда с code
    [HttpGet("instagram/callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] Guid state) // state = tenantId
    {
        
        _logger.LogInformation("Auth callback request: code={code}, state={state}", code, state);
        
        var appId = _config["Meta:AppId"];
        var appSecret = _config["Meta:AppSecret"];
        var redirectUri = _config["Meta:RedirectUri"];

        // Обмениваем code → short-lived token
        var tokenUrl = $"https://graph.facebook.com/v25.0/oauth/access_token"
            + $"?client_id={appId}"
            + $"&client_secret={appSecret}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&code={code}";

        var tokenResp = await _http.GetFromJsonAsync<TokenResponse>(tokenUrl);

        // Обмениваем → long-lived token
        var longLivedUrl = $"https://graph.facebook.com/v25.0/oauth/access_token"
            + $"?grant_type=fb_exchange_token"
            + $"&client_id={appId}"
            + $"&client_secret={appSecret}"
            + $"&fb_exchange_token={tokenResp.AccessToken}";

        var longLived = await _http.GetFromJsonAsync<TokenResponse>(longLivedUrl);

        // Получаем список страниц клиента
        var pagesUrl = $"https://graph.facebook.com/v25.0/me/accounts"
            + $"?access_token={longLived.AccessToken}";

        var pages = await _http.GetFromJsonAsync<PagesResponse>(pagesUrl);
        var page = pages.Data.First(); // или дать выбрать

        // Сохраняем в тенант
        var tenant = await _db.Tenants.FindAsync(state);
        tenant.InstagramPageId = page.Id;
        tenant.AccessToken = page.AccessToken; // Page-level token
        await _db.SaveChangesAsync();

        // Подписываем страницу на вебхуки
        await _http.PostAsync(
            $"https://graph.facebook.com/v25.0/{page.Id}/subscribed_apps"
            + $"?subscribed_fields=messages,messaging_postbacks"
            + $"&access_token={page.AccessToken}", null);

        return Ok("Instagram подключён!");
    }
}