using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class AuthService
{
    private readonly AuthDbContext _context;
    private readonly ApplicationService _applicationService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context,
        ApplicationService applicationService,
        JwtTokenService jwtTokenService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _applicationService = applicationService;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<TokenResponse?> RegisterUserAsync(RegisterRequest request)
    {
        try
        {
            // Validate application exists and is active
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null || !application.IsActive)
            {
                _logger.LogWarning("Registration attempt for invalid or inactive application: {ApplicationCode}", request.ApplicationCode);
                return null;
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpper() && u.ApplicationId == application.Id);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt for existing user: {Email} in application: {ApplicationCode}",
                    request.Email, request.ApplicationCode);
                return null;
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                ApplicationId = application.Id,
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                PasswordHash = HashPassword(request.Password),
                IsEmailVerified = false,
                PhoneNumber = request.PhoneNumber,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Assign single role
            if (!string.IsNullOrEmpty(request.Role))
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.ApplicationId == application.Id && r.Name == request.Role);

                if (role != null)
                {
                    user.RoleId = role.Id;
                    _logger.LogInformation("Assigned role {RoleName} to user {Email}", request.Role, user.Email);
                }
                else
                {
                    _logger.LogWarning("Role {RoleName} not found for application {ApplicationCode}", request.Role, application.Code);
                }
            }
            else
            {
                // Assign default "User" role if exists
                var defaultRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.ApplicationId == application.Id && r.Name.ToLower() == "user");

                if (defaultRole != null)
                {
                    user.RoleId = defaultRole.Id;
                    _logger.LogInformation("Assigned default 'User' role to {Email}", user.Email);
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send email verification
            await SendEmailVerificationAsync(request.Email, application.Id);

            _logger.LogInformation("User registered successfully: {Email} in application: {ApplicationCode}",
                user.Email, application.Code);

            // Generate tokens
            var roles = await GetUserRolesAsync(user.Id);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, application, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ApplicationId = application.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // TODO: Configure
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = "Registration"
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 15 * 60, // 15 minutes
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = new TokenResponse.UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    ApplicationId = application.Id,
                    ApplicationCode = application.Code,
                    ApplicationName = application.Name,
                    Role = roles,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}", request.Email);
            return null;
        }
    }

    public async Task<TokenResponse?> LoginUserAsync(LoginRequest request)
    {
        try
        {
            // Validate application exists and is active
            var application = await _applicationService.GetApplicationByCodeAsync(request.ApplicationCode);
            if (application == null || !application.IsActive)
            {
                _logger.LogWarning("Login attempt for invalid or inactive application: {ApplicationCode}", request.ApplicationCode);
                return null;
            }

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpper() && u.ApplicationId == application.Id);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt for non-existent or inactive user: {Email}", request.Email);
                return null;
            }

            // Validate password
            if (!ValidatePassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Email}", request.Email);

                // Log failed login attempt
                await LogSessionAsync(user.Id, application.Id, false, request.IpAddress, request.UserAgent);
                return null;
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log successful login
            await LogSessionAsync(user.Id, application.Id, true, request.IpAddress, request.UserAgent);

            // Generate tokens
            var roles = await GetUserRolesAsync(user.Id);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, application, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ApplicationId = application.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // TODO: Configure
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = request.DeviceInfo,
                IpAddress = request.IpAddress
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 15 * 60, // 15 minutes
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = new TokenResponse.UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    ApplicationId = application.Id,
                    ApplicationCode = application.Code,
                    ApplicationName = application.Name,
                    Role = roles,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user: {Email}", request.Email);
            return null;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool ValidatePassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    private async Task<string?> GetUserRolesAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Role?.Name;
    }

    private async Task LogSessionAsync(Guid userId, Guid applicationId, bool isSuccessful, string? ipAddress, string? userAgent)
    {
        var sessionLog = new SessionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = applicationId,
            LoginAt = isSuccessful ? DateTime.UtcNow : null,
            LogoutAt = null, // Will be set on logout
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccessful = isSuccessful
        };

        _context.SessionLogs.Add(sessionLog);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> LogoutUserAsync(string refreshToken)
    {
        try
        {
            // Find the refresh token
            var refreshTokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .Include(rt => rt.Application)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.RevokedAt.HasValue);

            if (refreshTokenEntity == null)
            {
                _logger.LogWarning("Logout attempt with invalid refresh token");
                return false;
            }

            // Revoke the refresh token
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Find the most recent login session for this user and mark it as logged out
            var recentSession = await _context.SessionLogs
                .Where(sl => sl.UserId == refreshTokenEntity.UserId &&
                           sl.ApplicationId == refreshTokenEntity.ApplicationId &&
                           sl.IsSuccessful &&
                           sl.LogoutAt == null)
                .OrderByDescending(sl => sl.LoginAt)
                .FirstOrDefaultAsync();

            if (recentSession != null)
            {
                recentSession.LogoutAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("User {UserId} logged out successfully", refreshTokenEntity.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user logout");
            return false;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string email, Guid applicationId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper() && u.ApplicationId == applicationId);

            if (user == null)
            {
                _logger.LogWarning("Email verification requested for non-existent user: {Email}", email);
                return false; // Don't reveal if user exists or not
            }

            if (user.IsEmailVerified)
            {
                _logger.LogInformation("Email already verified for user: {Email}", email);
                return true;
            }

            // Generate verification token
            var token = GenerateSecureToken();
            var emailToken = new EmailToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                TokenType = "email_verification",
                ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hours
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.EmailTokens.Add(emailToken);
            await _context.SaveChangesAsync();

            // Send verification email
            await _emailService.SendVerificationEmailAsync(email, token);

            _logger.LogInformation("Email verification sent to: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification for: {Email}", email);
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(string email, string token)
    {
        try
        {
            var emailToken = await _context.EmailTokens
                .Include(et => et.User)
                .FirstOrDefaultAsync(et =>
                    et.User.NormalizedEmail == email.ToUpper() &&
                    et.Token == token &&
                    et.TokenType == "email_verification" &&
                    !et.IsUsed &&
                    et.ExpiresAt > DateTime.UtcNow);

            if (emailToken == null)
            {
                _logger.LogWarning("Invalid or expired email verification token for: {Email}", email);
                return false;
            }

            // Mark email as verified
            emailToken.User.IsEmailVerified = true;
            emailToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified successfully for: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for: {Email}", email);
            return false;
        }
    }

    public async Task<bool> RequestPasswordResetAsync(string email, Guid applicationId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper() && u.ApplicationId == applicationId);

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent user: {Email}", email);
                return true; // Don't reveal if user exists or not
            }

            // Generate reset token
            var token = GenerateSecureToken();
            var emailToken = new EmailToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                TokenType = "password_reset",
                ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.EmailTokens.Add(emailToken);
            await _context.SaveChangesAsync();

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(email, token);

            _logger.LogInformation("Password reset email sent to: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset for: {Email}", email);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var emailToken = await _context.EmailTokens
                .Include(et => et.User)
                .FirstOrDefaultAsync(et =>
                    et.User.NormalizedEmail == email.ToUpper() &&
                    et.Token == token &&
                    et.TokenType == "password_reset" &&
                    !et.IsUsed &&
                    et.ExpiresAt > DateTime.UtcNow);

            if (emailToken == null)
            {
                _logger.LogWarning("Invalid or expired password reset token for: {Email}", email);
                return false;
            }

            // Update password
            emailToken.User.PasswordHash = HashPassword(newPassword);
            emailToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successfully for: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for: {Email}", email);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change attempted for non-existent user: {UserId}", userId);
                return false;
            }

            // Verify current password
            if (!ValidatePassword(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                return false;
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    private string GenerateSecureToken()
    {
        // Generate a secure random token
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
