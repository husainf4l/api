using AuthService.Services;

namespace AuthService.Middleware;

/// <summary>
/// Middleware to validate API keys from x-api-key header
/// This allows other microservices to authenticate using API keys
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Check if request has x-api-key header
        if (context.Request.Headers.TryGetValue("x-api-key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var validatedKey = await apiKeyService.ValidateApiKeyAsync(apiKey);

                    if (validatedKey != null)
                    {
                        // Store API key info in HttpContext for controllers to use
                        context.Items["ApiKeyId"] = validatedKey.Id;
                        context.Items["UserId"] = validatedKey.UserId;
                        context.Items["UserEmail"] = validatedKey.User?.Email;
                        context.Items["ApiKeyScopes"] = validatedKey.Scopes;
                        context.Items["ApplicationId"] = validatedKey.ApplicationId;
                        context.Items["AuthMethod"] = "ApiKey";

                        // Update last used
                        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        await apiKeyService.UpdateLastUsedAsync(validatedKey.Id, ipAddress);

                        _logger.LogInformation("Request authenticated with API key: {KeyId}", validatedKey.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid API key provided from IP: {IP}", 
                            context.Connection.RemoteIpAddress?.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating API key");
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register ApiKeyMiddleware
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
