using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public interface IExternalLoginService
{
    Task<ExternalLoginResult> InitiateExternalLoginAsync(string provider, Guid applicationId);
    Task<ExternalLoginResult> HandleExternalLoginCallbackAsync(string provider, Guid applicationId, string providerUserId, string? email, string? name, string? accessToken = null, string? refreshToken = null);
    Task<ExternalLoginResult> LinkExternalLoginAsync(Guid userId, string provider, string providerUserId, string? email, string? name);
    Task<bool> UnlinkExternalLoginAsync(Guid userId, string provider);
    Task<List<ExternalLoginInfoDto>> GetUserExternalLoginsAsync(Guid userId);
    Task<bool> IsExternalLoginLinkedAsync(Guid userId, string provider);
}

public class ExternalLoginService : IExternalLoginService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<ExternalLoginService> _logger;

    public ExternalLoginService(AuthDbContext context, ILogger<ExternalLoginService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExternalLoginResult> InitiateExternalLoginAsync(string provider, Guid applicationId)
    {
        try
        {
            // Validate provider is supported
            if (!IsProviderSupported(provider))
            {
                return new ExternalLoginResult
                {
                    Success = false,
                    Error = $"Provider '{provider}' is not supported"
                };
            }

            // Generate state parameter for security
            var state = GenerateSecureState(applicationId);

            // For API-based OAuth, we would return the authorization URL
            // For now, we'll indicate that the client should redirect to the OAuth provider
            var authorizationUrl = GenerateAuthorizationUrl(provider, state);

            return new ExternalLoginResult
            {
                Success = true,
                RedirectUrl = authorizationUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating external login for provider {Provider}", provider);
            return new ExternalLoginResult
            {
                Success = false,
                Error = "Failed to initiate external login"
            };
        }
    }

    public async Task<ExternalLoginResult> HandleExternalLoginCallbackAsync(
        string provider,
        Guid applicationId,
        string providerUserId,
        string? email,
        string? name,
        string? accessToken = null,
        string? refreshToken = null)
    {
        try
        {
            // Check if external login already exists
            var existingExternalLogin = await _context.UserExternalLogins
                .Include(el => el.User)
                .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderUserId == providerUserId);

            if (existingExternalLogin != null)
            {
                // User has already linked this external login - sign them in
                return await SignInWithExternalLoginAsync(existingExternalLogin, applicationId, accessToken, refreshToken);
            }

            // Check if a user with this email already exists
            if (!string.IsNullOrEmpty(email))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper() && u.ApplicationId == applicationId);

                if (existingUser != null)
                {
                    // User exists but hasn't linked this external login
                    // Return info for account linking flow
                    return new ExternalLoginResult
                    {
                        Success = true,
                        RequiresAccountLinking = true,
                        ExternalLoginInfo = new ExternalLoginInfoDto
                        {
                            Provider = provider,
                            ProviderUserId = providerUserId,
                            ProviderUserEmail = email,
                            ProviderUserName = name
                        }
                    };
                }
            }

            // Create new user account
            return await CreateUserWithExternalLoginAsync(provider, applicationId, providerUserId, email, name, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling external login callback for provider {Provider}", provider);
            return new ExternalLoginResult
            {
                Success = false,
                Error = "Failed to process external login"
            };
        }
    }

    public async Task<ExternalLoginResult> LinkExternalLoginAsync(Guid userId, string provider, string providerUserId, string? email, string? name)
    {
        try
        {
            // Check if this external login is already linked to another user
            var existingLink = await _context.UserExternalLogins
                .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderUserId == providerUserId);

            if (existingLink != null)
            {
                return new ExternalLoginResult
                {
                    Success = false,
                    Error = "This external login is already linked to another account"
                };
            }

            // Check if user already has this provider linked
            var userExistingLink = await _context.UserExternalLogins
                .FirstOrDefaultAsync(el => el.UserId == userId && el.Provider == provider);

            if (userExistingLink != null)
            {
                return new ExternalLoginResult
                {
                    Success = false,
                    Error = $"Account is already linked to {provider}"
                };
            }

            // Create the external login link
            var externalLogin = new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProviderUserEmail = email,
                ProviderUserName = name,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.UserExternalLogins.Add(externalLogin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("External login {Provider} linked to user {UserId}", provider, userId);

            return new ExternalLoginResult
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external login for user {UserId}, provider {Provider}", userId, provider);
            return new ExternalLoginResult
            {
                Success = false,
                Error = "Failed to link external login"
            };
        }
    }

    public async Task<bool> UnlinkExternalLoginAsync(Guid userId, string provider)
    {
        try
        {
            var externalLogin = await _context.UserExternalLogins
                .FirstOrDefaultAsync(el => el.UserId == userId && el.Provider == provider);

            if (externalLogin == null)
            {
                return false;
            }

            // Check if user has a password or other external logins before unlinking
            var user = await _context.Users.FindAsync(userId);
            var otherExternalLogins = await _context.UserExternalLogins
                .CountAsync(el => el.UserId == userId && el.Provider != provider);

            if (user == null)
            {
                return false;
            }

            // If no password and no other external logins, prevent unlinking
            if (string.IsNullOrEmpty(user.PasswordHash) && otherExternalLogins == 0)
            {
                _logger.LogWarning("Cannot unlink external login {Provider} for user {UserId} - no other login methods", provider, userId);
                return false;
            }

            _context.UserExternalLogins.Remove(externalLogin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("External login {Provider} unlinked from user {UserId}", provider, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking external login for user {UserId}, provider {Provider}", userId, provider);
            return false;
        }
    }

    public async Task<List<ExternalLoginInfoDto>> GetUserExternalLoginsAsync(Guid userId)
    {
        try
        {
            var externalLogins = await _context.UserExternalLogins
                .Where(el => el.UserId == userId)
                .OrderBy(el => el.Provider)
                .ToListAsync();

            return externalLogins.Select(el => new ExternalLoginInfoDto
            {
                Provider = el.Provider,
                ProviderUserId = el.ProviderUserId,
                ProviderUserEmail = el.ProviderUserEmail,
                ProviderUserName = el.ProviderUserName,
                LastLoginAt = el.LastLoginAt,
                CreatedAt = el.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external logins for user {UserId}", userId);
            return new List<ExternalLoginInfoDto>();
        }
    }

    public async Task<bool> IsExternalLoginLinkedAsync(Guid userId, string provider)
    {
        try
        {
            return await _context.UserExternalLogins
                .AnyAsync(el => el.UserId == userId && el.Provider == provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external login link for user {UserId}, provider {Provider}", userId, provider);
            return false;
        }
    }

    private async Task<ExternalLoginResult> SignInWithExternalLoginAsync(
        UserExternalLogin externalLogin,
        Guid applicationId,
        string? accessToken,
        string? refreshToken)
    {
        try
        {
            // Update last login
            externalLogin.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(accessToken))
            {
                externalLogin.AccessToken = accessToken;
            }
            if (!string.IsNullOrEmpty(refreshToken))
            {
                externalLogin.RefreshToken = refreshToken;
            }

            await _context.SaveChangesAsync();

            // Generate JWT tokens (you would inject and use AuthService here)
            // For now, return success with external login info
            return new ExternalLoginResult
            {
                Success = true,
                ExternalLoginInfo = new ExternalLoginInfoDto
                {
                    Provider = externalLogin.Provider,
                    ProviderUserId = externalLogin.ProviderUserId,
                    ProviderUserEmail = externalLogin.ProviderUserEmail,
                    ProviderUserName = externalLogin.ProviderUserName,
                    LastLoginAt = externalLogin.LastLoginAt,
                    CreatedAt = externalLogin.CreatedAt
                }
                // TokenResponse would be generated by AuthService
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing in with external login");
            return new ExternalLoginResult
            {
                Success = false,
                Error = "Failed to sign in with external login"
            };
        }
    }

    private async Task<ExternalLoginResult> CreateUserWithExternalLoginAsync(
        string provider,
        Guid applicationId,
        string providerUserId,
        string? email,
        string? name,
        string? accessToken,
        string? refreshToken)
    {
        try
        {
            // Generate a unique email if not provided
            var userEmail = email ?? $"{providerUserId}@{provider.ToLower()}.local";

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Email = userEmail,
                NormalizedEmail = userEmail.ToUpper(),
                IsEmailVerified = !string.IsNullOrEmpty(email), // Verified if email came from OAuth provider
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);

            // Create external login link
            var externalLogin = new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProviderUserEmail = email,
                ProviderUserName = name,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.UserExternalLogins.Add(externalLogin);

            // Assign default role
            var defaultRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.Name.ToLower() == "user");

            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id
                };
                _context.UserRoles.Add(userRole);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("New user created via {Provider} OAuth: {Email}", provider, userEmail);

            return new ExternalLoginResult
            {
                Success = true,
                ExternalLoginInfo = new ExternalLoginInfoDto
                {
                    Provider = provider,
                    ProviderUserId = providerUserId,
                    ProviderUserEmail = email,
                    ProviderUserName = name,
                    CreatedAt = DateTime.UtcNow
                }
                // TokenResponse would be generated here
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with external login");
            return new ExternalLoginResult
            {
                Success = false,
                Error = "Failed to create account with external login"
            };
        }
    }

    private bool IsProviderSupported(string provider)
    {
        return provider.ToLower() switch
        {
            "google" => true,
            "github" => true,
            "microsoft" => true,
            "apple" => true,
            _ => false
        };
    }

    private string GenerateSecureState(Guid applicationId)
    {
        var stateData = $"{applicationId}:{DateTime.UtcNow.Ticks}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(stateData);
        return Convert.ToBase64String(bytes);
    }

    private string GenerateAuthorizationUrl(string provider, string state)
    {
        // This would generate the actual OAuth authorization URL
        // For now, return a placeholder
        return $"/auth/external-login/{provider.ToLower()}?state={Uri.EscapeDataString(state)}";
    }
}
