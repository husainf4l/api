using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Services.IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly IConfiguration _configuration;
    private readonly int _loginRateLimit;
    private readonly TimeSpan _loginRateWindow;
    private readonly int _refreshRateLimit;
    private readonly TimeSpan _refreshRateWindow;

    public AuthController(
        Services.IAuthService authService,
        ILogger<AuthController> logger,
        IRateLimiter rateLimiter,
        IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _rateLimiter = rateLimiter;
        _configuration = configuration;
        _loginRateLimit = _configuration.GetValue("Security:LoginRateLimit:Requests", 10);
        _loginRateWindow = TimeSpan.FromSeconds(_configuration.GetValue("Security:LoginRateLimit:WindowSeconds", 60));
        _refreshRateLimit = _configuration.GetValue("Security:RefreshRateLimit:Requests", 30);
        _refreshRateWindow = TimeSpan.FromSeconds(_configuration.GetValue("Security:RefreshRateLimit:WindowSeconds", 60));
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for email: {Email}", request.Email);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ipAllowed = IsAllowed("login:ip", ipAddress, _loginRateLimit, _loginRateWindow, out var retryAfterIp);
            var userAllowed = IsAllowed("login:user", request.Email.ToLowerInvariant(), _loginRateLimit, _loginRateWindow, out var retryAfterUser);
            
            if (!ipAllowed || !userAllowed)
            {
                var retryAfter = retryAfterIp > retryAfterUser ? retryAfterIp : retryAfterUser;
                Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
                _logger.LogWarning("Rate limit hit for login. IP: {Ip} Email: {Email}", ipAddress, request.Email);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many login attempts. Try again later." });
            }

            var response = await _authService.LoginAsync(request, ipAddress);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for email: {Email}", request.Email);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!IsAllowed("refresh:ip", ipAddress, _refreshRateLimit, _refreshRateWindow, out var retryAfter))
            {
                Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
                _logger.LogWarning("Rate limit hit for refresh. IP: {Ip}", ipAddress);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many refresh attempts. Try again later." });
            }

            var response = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Revoke a refresh token (logout)
    /// </summary>
    [HttpPost("revoke")]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);
            
            if (!result)
            {
                return BadRequest(new { message = "Invalid token" });
            }

            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, new { message = "An error occurred during token revocation" });
        }
    }

    /// <summary>
    /// Validate access token and get user info
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<UserResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var user = await _authService.ValidateTokenAsync(request.AccessToken);
            
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return StatusCode(500, new { message = "An error occurred during token validation" });
        }
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Middleware.Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token == null)
            {
                return Unauthorized(new { message = "No token provided" });
            }

            var user = await _authService.ValidateTokenAsync(token);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    private bool IsAllowed(string prefix, string value, int limit, TimeSpan window, out TimeSpan retryAfter)
    {
        var key = $"{prefix}:{value}";
        return _rateLimiter.TryAcquire(key, limit, window, out retryAfter);
    }
}

public class ValidateTokenRequest
{
    public required string AccessToken { get; set; }
}
