using HotChocolate.Resolvers;

namespace SmsService.GraphQL.Middleware;

public class ApiKeyAuthorizationMiddleware
{
    private readonly FieldDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthorizationMiddleware> _logger;

    public ApiKeyAuthorizationMiddleware(
        FieldDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthorizationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        // Get the field name being accessed
        var fieldName = context.Selection.Field.Name.ToLower();
        
        // Public fields that don't require authentication
        var publicFields = new HashSet<string> { "health", "senders", "__schema", "__type" };
        
        if (publicFields.Contains(fieldName))
        {
            await _next(context);
            return;
        }

        // Check for API key in headers
        var httpContext = context.Services.GetRequiredService<IHttpContextAccessor>().HttpContext;
        
        if (httpContext == null)
        {
            context.Result = ErrorBuilder.New()
                .SetMessage("HTTP context not available")
                .Build();
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Result = ErrorBuilder.New()
                .SetMessage("API Key is missing")
                .SetCode("AUTH_NOT_AUTHENTICATED")
                .Build();
            return;
        }

        var apiKey = _configuration["ApiSettings:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey) || extractedApiKey != apiKey)
        {
            _logger.LogWarning("Invalid API key attempt for field: {FieldName}", fieldName);
            context.Result = ErrorBuilder.New()
                .SetMessage("Invalid API Key")
                .SetCode("AUTH_NOT_AUTHORIZED")
                .Build();
            return;
        }

        await _next(context);
    }
}
