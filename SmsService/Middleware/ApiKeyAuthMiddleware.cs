namespace SmsService.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check endpoint
        if (context.Request.Path.StartsWithSegments("/api/sms/health"))
        {
            await _next(context);
            return;
        }

        // Skip authentication for non-SMS API endpoints
        if (!context.Request.Path.StartsWithSegments("/api/sms"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing" });
            return;
        }

        var apiKey = _configuration["ApiSettings:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("API Key not configured in settings");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "API Key not configured" });
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            _logger.LogWarning("Invalid API Key attempt from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        await _next(context);
    }
}
