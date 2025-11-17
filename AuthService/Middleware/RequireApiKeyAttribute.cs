using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthService.Middleware;

/// <summary>
/// Requires a valid API key in the X-API-Key header for the request
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get API key from header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValue))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "API key is required. Please provide X-API-Key header."
            });
            return;
        }

        // Get services
        var apiKeyService = context.HttpContext.RequestServices.GetRequiredService<ApiKeyService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireApiKeyAttribute>>();

        // Validate API key
        var validationResult = await apiKeyService.ValidateApiKeyAsync(apiKeyValue!);
        
        if (validationResult == null || !validationResult.IsValid)
        {
            logger.LogWarning("Invalid API key attempt: {ApiKey}", apiKeyValue.ToString() ?? "null");
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = validationResult?.Message ?? "Invalid or expired API key"
            });
            return;
        }

        // Store API key info in HttpContext for later use
        context.HttpContext.Items["ApplicationId"] = validationResult.ApplicationId;
        context.HttpContext.Items["ApiKeyUserId"] = validationResult.UserId;
        context.HttpContext.Items["ApiKeyScopes"] = validationResult.Scopes;

        logger.LogInformation("API key validated for application {AppId}", validationResult.ApplicationId);

        await next();
    }
}
