using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WhatsApp.Constants;

namespace WhatsApp.Services;

public class TokenManagerService
{
    private readonly ILogger<TokenManagerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "whatsapp_access_token";

    public TokenManagerService(
        ILogger<TokenManagerService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        var newToken = await FetchAccessTokenAsync();
        return newToken;
    }

    private async Task<string> FetchAccessTokenAsync()
    {
        try
        {
            // Try static token first
            var staticToken = _configuration["WHATSAPP_ACCESS_TOKEN"];
            if (!string.IsNullOrEmpty(staticToken))
            {
                _logger.LogInformation("Using static WhatsApp access token");
                CacheToken(staticToken, TimeConstants.WHATSAPP_TOKEN_DURATION);
                return staticToken;
            }

            // Try system user token from environment
            var envSystemUserToken = _configuration["FACEBOOK_SYSTEM_USER_TOKEN"];
            if (!string.IsNullOrEmpty(envSystemUserToken))
            {
                _logger.LogInformation("Using FACEBOOK_SYSTEM_USER_TOKEN from environment");
                CacheToken(envSystemUserToken, TimeConstants.WHATSAPP_TOKEN_DURATION);
                return envSystemUserToken;
            }

            // Generate token dynamically
            var appId = _configuration["FACEBOOK_APP_ID"];
            var appSecret = _configuration["FACEBOOK_APP_SECRET"];
            var systemUserId = _configuration["FACEBOOK_SYSTEM_USER_ID"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(systemUserId))
            {
                throw new InvalidOperationException(
                    "Missing Facebook app credentials. Please set FACEBOOK_APP_ID, FACEBOOK_APP_SECRET, and FACEBOOK_SYSTEM_USER_ID");
            }

            var appAccessToken = await GetAppAccessTokenAsync(appId, appSecret);
            var systemUserToken = await GetSystemUserTokenAsync(systemUserId, appAccessToken);

            CacheToken(systemUserToken, TimeConstants.WHATSAPP_TOKEN_DURATION);
            _logger.LogInformation("Successfully fetched new WhatsApp access token");

            return systemUserToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch access token");
            throw new InvalidOperationException("Unable to obtain WhatsApp access token", ex);
        }
    }

    private async Task<string> GetAppAccessTokenAsync(string appId, string appSecret)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://graph.facebook.com/v23.0/oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get app access token: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        if (data == null || !data.ContainsKey("access_token"))
        {
            throw new InvalidOperationException("Invalid response from Facebook API");
        }

        return data["access_token"].ToString()!;
    }

    private async Task<string> GetSystemUserTokenAsync(string systemUserId, string appAccessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://graph.facebook.com/v23.0/{systemUserId}/access_tokens";

        var requestBody = JsonSerializer.Serialize(new
        {
            scope = "whatsapp_business_messaging,whatsapp_business_management"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appAccessToken);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get system user token: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        if (data == null || !data.ContainsKey("access_token"))
        {
            throw new InvalidOperationException("Invalid response from Facebook API");
        }

        return data["access_token"].ToString()!;
    }

    public async Task<string> GetWhatsAppBusinessAccountTokenAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var wabaId = _configuration["WHATSAPP_BUSINESS_ACCOUNT_ID"];

            if (string.IsNullOrEmpty(wabaId))
            {
                return accessToken;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/v23.0/{wabaId}?fields=access_token";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not get WABA-specific token, using system user token");
                return accessToken;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data != null && data.ContainsKey("access_token"))
            {
                return data["access_token"].ToString()!;
            }

            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WABA token");
            return await GetAccessTokenAsync();
        }
    }

    private void CacheToken(string token, long ttlMs)
    {
        var expiresAt = DateTimeOffset.Now.AddMilliseconds(ttlMs);
        _cache.Set(CacheKey, token, expiresAt);
    }

    public void ClearTokenCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Token cache cleared");
    }

    public async Task<object?> GetTokenInfoAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/v23.0/debug_token?input_token={token}&access_token={token}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token info");
        }

        return null;
    }
}
