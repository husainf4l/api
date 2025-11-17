using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Middleware;

/// <summary>
/// Requires the authenticated user to have specific roles
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _requiredRoles;

    public RequireRoleAttribute(params string[] roles)
    {
        _requiredRoles = roles ?? Array.Empty<string>();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Authentication required"
            });
            return;
        }

        // Get single role from JWT token claims
        var userRole = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "role")?.Value;

        if (string.IsNullOrEmpty(userRole))
        {
            logger.LogWarning("User has no role assigned");
            context.Result = new ForbiddenObjectResult(new
            {
                success = false,
                message = "Access denied. No role assigned to user."
            });
            return;
        }

        // Check if user has any of the required roles
        var hasRequiredRole = _requiredRoles.Length == 0 || 
                             _requiredRoles.Any(role => role.Equals(userRole, StringComparison.OrdinalIgnoreCase));

        if (!hasRequiredRole)
        {
            logger.LogWarning("User with role [{UserRole}] attempted to access endpoint requiring [{RequiredRoles}]",
                userRole, string.Join(", ", _requiredRoles));
            
            context.Result = new ForbiddenObjectResult(new
            {
                success = false,
                message = $"Access denied. Required roles: {string.Join(", ", _requiredRoles)}",
                userRole = userRole,
                requiredRoles = _requiredRoles
            });
            return;
        }

        logger.LogInformation("User with role [{Role}] authorized for endpoint", userRole);
        await next();
    }
}

// Custom result for 403 Forbidden
public class ForbiddenObjectResult : ObjectResult
{
    public ForbiddenObjectResult(object value) : base(value)
    {
        StatusCode = StatusCodes.Status403Forbidden;
    }
}
