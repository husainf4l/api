using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class TokenController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        AuthDbContext context,
        JwtTokenService jwtTokenService,
        ILogger<TokenController> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Refresh token is required"
                });
            }

            // Find the refresh token in database
            var refreshTokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .Include(rt => rt.Application)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.RevokedAt.HasValue);

            if (refreshTokenEntity == null || refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid or expired refresh token"
                });
            }

            // Revoke the current refresh token (one-time use)
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            refreshTokenEntity.ReplacedByToken = "new-token"; // Will be set below

            // Generate new tokens
            var user = refreshTokenEntity.User;
            var application = refreshTokenEntity.Application;

            // Get user roles
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var accessToken = _jwtTokenService.GenerateAccessToken(user, application, roles);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store new refresh token
            var newRefreshTokenEntity = new Models.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ApplicationId = application.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // TODO: Configure
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = refreshTokenEntity.DeviceInfo,
                IpAddress = GetClientIpAddress()
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);

            // Update the old token's replacement reference
            refreshTokenEntity.ReplacedByToken = newRefreshToken;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed successfully for user: {Email}", user.Email);

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                data = new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = 15 * 60, // 15 minutes
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    User = new TokenResponse.UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        ApplicationId = application.Id,
                        ApplicationCode = application.Code,
                        ApplicationName = application.Name,
                        Roles = roles,
                        IsEmailVerified = user.IsEmailVerified,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during token refresh"
            });
        }
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded headers first (when behind proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}
