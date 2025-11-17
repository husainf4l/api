using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly Services.AuthService _authService;
    private readonly ApplicationService _applicationService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IExternalLoginService _externalLoginService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        Services.AuthService authService,
        ApplicationService applicationService,
        JwtTokenService jwtTokenService,
        ITwoFactorService twoFactorService,
        IExternalLoginService externalLoginService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _applicationService = applicationService;
        _jwtTokenService = jwtTokenService;
        _twoFactorService = twoFactorService;
        _externalLoginService = externalLoginService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var result = await _authService.RegisterUserAsync(request);

            if (result == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Registration failed. User may already exist or application is invalid."
                });
            }

            return Ok(new
            {
                success = true,
                message = "User registered successfully",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during registration"
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var result = await _authService.LoginUserAsync(request);

            if (result == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email, password, or application code"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during login"
            });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // Extract token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Authorization token required"
                });
            }

            var token = authHeader.Substring("Bearer ".Length);

            // Validate token
            var principal = _jwtTokenService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid or expired token"
                });
            }

            // Extract user information from token
            var userId = _jwtTokenService.GetUserIdFromToken(token);
            var applicationId = _jwtTokenService.GetApplicationIdFromToken(token);
            var roles = _jwtTokenService.GetRolesFromToken(token);

            if (!userId.HasValue || !applicationId.HasValue)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token claims"
                });
            }

            // Get additional user details from database if needed
            // For now, return information from token

            var claims = _jwtTokenService.ExtractClaims(token);

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = userId.Value,
                    email = claims.GetValueOrDefault(JwtRegisteredClaimNames.Email, ""),
                    application_id = applicationId.Value,
                    application_code = claims.GetValueOrDefault("app", ""),
                    application_name = claims.GetValueOrDefault("app_name", ""),
                    roles = roles,
                    is_email_verified = false, // TODO: Get from database
                    created_at = DateTime.UtcNow, // TODO: Get from database
                    last_login_at = DateTime.UtcNow // TODO: Get from database
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
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

            var success = await _authService.LogoutUserAsync(request.RefreshToken);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid refresh token or logout failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = "User logged out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user logout");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during logout"
            });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var success = await _authService.VerifyEmailAsync(request.Email, request.Token);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid or expired verification token"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Email verified successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during email verification"
            });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Get application
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid application code"
                });
            }

            var success = await _authService.SendEmailVerificationAsync(request.Email, application.Id);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to send verification email"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Verification email sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Get application
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid application code"
                });
            }

            var success = await _authService.RequestPasswordResetAsync(request.Email, application.Id);

            // Always return success to prevent email enumeration
            return Ok(new
            {
                success = true,
                message = "If the email exists, a password reset link has been sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var success = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid or expired reset token"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Password reset successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during password reset"
            });
        }
    }

    [HttpPost("change-password")]
    [Authorize] // Requires authentication
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Get current user from JWT token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid user token"
                });
            }

            // Get application
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid application code"
                });
            }

            var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Current password is incorrect"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Password changed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during password change"
            });
        }
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> SetupTwoFactor([FromBody] TwoFactorSetupRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var result = await _twoFactorService.SetupTwoFactorAsync(userId, "Auth Service");

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Error ?? "Failed to setup two-factor authentication"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Two-factor authentication setup initiated",
                data = new TwoFactorSetupResponse
                {
                    Success = true,
                    Secret = result.Secret,
                    QrCodeUri = result.QrCodeUri,
                    QrCodeImageBase64 = result.QrCodeImage != null ? Convert.ToBase64String(result.QrCodeImage) : null,
                    BackupCodes = result.BackupCodes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up two-factor authentication");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorEnableRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var success = await _twoFactorService.EnableTwoFactorAsync(userId, request.VerificationCode);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid verification code"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Two-factor authentication enabled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorSetupRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var success = await _twoFactorService.DisableTwoFactorAsync(userId);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to disable two-factor authentication"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Two-factor authentication disabled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var status = await _twoFactorService.GetTwoFactorStatusAsync(userId);

            return Ok(new
            {
                success = true,
                data = new TwoFactorStatusResponse
                {
                    IsEnabled = status.IsEnabled,
                    IsConfigured = status.IsConfigured,
                    HasBackupCodes = status.HasBackupCodes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting two-factor status");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        try
        {
            // For verification during login, we need to find the user first
            // This endpoint is used when 2FA is required during authentication
            // Implementation depends on your specific login flow

            return Ok(new
            {
                success = true,
                message = "Two-factor verification endpoint - implement based on your login flow"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("2fa/regenerate-backup")]
    [Authorize]
    public async Task<IActionResult> RegenerateBackupCodes([FromBody] TwoFactorSetupRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var success = await _twoFactorService.RegenerateBackupCodesAsync(userId);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to regenerate backup codes"
                });
            }

            // Note: In a real implementation, you'd return the new backup codes
            // But for security, you might want to show them only once during initial setup
            return Ok(new
            {
                success = true,
                message = "Backup codes regenerated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating backup codes");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("external-login")]
    public async Task<IActionResult> InitiateExternalLogin([FromBody] ExternalLoginRequest request)
    {
        try
        {
            // Get application
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid application code"
                });
            }

            var result = await _externalLoginService.InitiateExternalLoginAsync(request.Provider, application.Id);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Error ?? "Failed to initiate external login"
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Redirect to {request.Provider} for authentication",
                data = new
                {
                    provider = request.Provider,
                    authorizationUrl = result.RedirectUrl
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating external login");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("external-callback/{provider}")]
    public async Task<IActionResult> ExternalLoginCallback(string provider)
    {
        try
        {
            // This endpoint handles the OAuth callback from external providers
            // In a real implementation, this would be called by the OAuth provider
            // For now, return instructions
            return Ok(new
            {
                message = $"External login callback for {provider}",
                note = "This endpoint should be configured as the OAuth callback URL in your OAuth provider settings"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling external login callback");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("link-external-login")]
    [Authorize]
    public async Task<IActionResult> LinkExternalLogin([FromBody] LinkExternalLoginRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            // For account linking, we would typically redirect to OAuth provider first
            // This is a simplified version
            return Ok(new
            {
                success = true,
                message = $"To link {request.Provider}, visit the external login URL first",
                provider = request.Provider
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external login");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("unlink-external-login")]
    [Authorize]
    public async Task<IActionResult> UnlinkExternalLogin([FromBody] UnlinkExternalLoginRequest request)
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var success = await _externalLoginService.UnlinkExternalLoginAsync(userId, request.Provider);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to unlink external login"
                });
            }

            return Ok(new
            {
                success = true,
                message = $"{request.Provider} account unlinked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking external login");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("external-logins")]
    [Authorize]
    public async Task<IActionResult> GetExternalLogins()
    {
        try
        {
            // Get current user
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user token" });
            }

            var externalLogins = await _externalLoginService.GetUserExternalLoginsAsync(userId);

            return Ok(new
            {
                success = true,
                data = externalLogins
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external logins");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
