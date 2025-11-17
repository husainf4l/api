using System.Security.Claims;
using AuthService.Services;

namespace AuthService.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            AttachUserToContext(context, tokenService, token);
        }

        await _next(context);
    }

    private void AttachUserToContext(HttpContext context, ITokenService tokenService, string token)
    {
        try
        {
            var principal = tokenService.ValidateAccessToken(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }
        catch
        {
            // Token validation failed, do nothing
        }
    }
}
