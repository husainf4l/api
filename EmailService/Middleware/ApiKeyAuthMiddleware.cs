using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EmailService.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health check endpoint
            if (context.Request.Path.StartsWithSegments("/api/email/health"))
            {
                await _next(context);
                return;
            }

            // Skip authentication for non-API routes
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "API Key is missing" });
                return;
            }

            var apiKey = _configuration["ApiSettings:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "API Key not configured on server" });
                return;
            }

            if (!apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
                return;
            }

            await _next(context);
        }
    }
}
