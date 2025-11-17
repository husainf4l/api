using System.Collections.Generic;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.AspNetCore.Http;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress);
    Task<UserResponse?> ValidateTokenAsync(string accessToken);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IAuditLogger _auditLogger;
    private readonly int _refreshTokenExpiryDays;
    private readonly int _maxFailedLoginAttempts;
    private readonly TimeSpan _lockoutDuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordService passwordService,
        IConfiguration configuration,
        IPasswordPolicyValidator passwordPolicyValidator,
        IPasswordHistoryRepository passwordHistoryRepository,
        IRefreshTokenHasher refreshTokenHasher,
        IAuditLogger auditLogger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _configuration = configuration;
        _passwordPolicyValidator = passwordPolicyValidator;
        _passwordHistoryRepository = passwordHistoryRepository;
        _refreshTokenHasher = refreshTokenHasher;
        _auditLogger = auditLogger;
        _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
        _maxFailedLoginAttempts = int.Parse(_configuration["Security:AccountLockout:MaxFailedAttempts"] ?? "5");
        var lockoutMinutes = int.Parse(_configuration["Security:AccountLockout:LockoutMinutes"] ?? "15");
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        _passwordPolicyValidator.Validate(request.Password, request.Email);

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            PasswordUpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        await _passwordHistoryRepository.AddAsync(new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTime.UtcNow
        });
        await _auditLogger.LogAsync(AuditEventType.PasswordChanged, new AuditEventContext
        {
            UserId = user.Id,
            Email = user.Email,
            Extras = new Dictionary<string, object?>
            {
                ["event"] = "UserRegistered"
            }
        });

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = null
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new AuthResponse
        {
            Tokens = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15")),
                TokenType = "Bearer"
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        // Find user by email
        var normalizedEmail = request.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user == null)
        {
            await _auditLogger.LogAsync(AuditEventType.LoginFailed, new AuditEventContext
            {
                Email = normalizedEmail,
                IpAddress = ipAddress,
                DeviceInfo = request.DeviceInfo,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "UserNotFound"
                }
            });
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            await _auditLogger.LogAsync(AuditEventType.AccountLocked, new AuditEventContext
            {
                UserId = user.Id,
                Email = user.Email,
                IpAddress = ipAddress
            });
            throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await _auditLogger.LogAsync(AuditEventType.LoginFailed, new AuditEventContext
            {
                UserId = user.Id,
                Email = user.Email,
                IpAddress = ipAddress,
                DeviceInfo = request.DeviceInfo,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "InvalidPassword"
                }
            });
            await HandleFailedLoginAsync(user, ipAddress);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _userRepository.UpdateAsync(user);
        await _auditLogger.LogAsync(AuditEventType.LoginSucceeded, new AuditEventContext
        {
            UserId = user.Id,
            Email = user.Email,
            IpAddress = ipAddress,
            DeviceInfo = request.DeviceInfo
        });

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            DeviceInfo = request.DeviceInfo
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new AuthResponse
        {
            Tokens = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15")),
                TokenType = "Bearer"
            },
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        
        if (token == null)
        {
            await _auditLogger.LogAsync(AuditEventType.RefreshFailed, new AuditEventContext
            {
                IpAddress = ipAddress,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "TokenNotFound"
                }
            });
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (!token.IsActive)
        {
            if (token.IsRevoked && token.ReplacedByTokenHash != null)
            {
                await _refreshTokenRepository.MarkAsReusedAsync(token, ipAddress);
                await _refreshTokenRepository.RevokeAllUserTokensAsync(token.UserId, ipAddress);
            }

            await _auditLogger.LogAsync(AuditEventType.RefreshFailed, new AuditEventContext
            {
                UserId = token.UserId,
                IpAddress = ipAddress,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "TokenInactive"
                }
            });
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId);
        if (user == null || !user.IsActive)
        {
            await _auditLogger.LogAsync(AuditEventType.RefreshFailed, new AuditEventContext
            {
                UserId = token.UserId,
                IpAddress = ipAddress,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "UserInactive"
                }
            });
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        // Revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevokedReason = RefreshTokenRevocationReasons.Replaced;

        // Generate new tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshToken);

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            DeviceInfo = token.DeviceInfo
        };

        await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);
        token.ReplacedByTokenHash = newRefreshTokenHash;
        await _refreshTokenRepository.UpdateAsync(token);
        await _auditLogger.LogAsync(AuditEventType.RefreshSucceeded, new AuditEventContext
        {
            UserId = user.Id,
            Email = user.Email,
            IpAddress = ipAddress,
            DeviceInfo = token.DeviceInfo,
            Extras = new Dictionary<string, object?>
            {
                ["refreshTokenId"] = newRefreshTokenEntity.Id
            }
        });

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15")),
            TokenType = "Bearer"
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        
        if (token == null)
        {
            await _auditLogger.LogAsync(AuditEventType.RevokeFailed, new AuditEventContext
            {
                IpAddress = ipAddress,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "TokenNotFound"
                }
            });
            return false;
        }

        if (!token.IsActive)
        {
            await _auditLogger.LogAsync(AuditEventType.RevokeFailed, new AuditEventContext
            {
                UserId = token.UserId,
                IpAddress = ipAddress,
                Extras = new Dictionary<string, object?>
                {
                    ["reason"] = "TokenInactive"
                }
            });
            return false;
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevokedReason = RefreshTokenRevocationReasons.ManuallyRevoked;
        await _refreshTokenRepository.UpdateAsync(token);
        await _auditLogger.LogAsync(AuditEventType.RevokeSucceeded, new AuditEventContext
        {
            UserId = token.UserId,
            IpAddress = ipAddress
        });

        return true;
    }

    public async Task<UserResponse?> ValidateTokenAsync(string accessToken)
    {
        var principal = _tokenService.ValidateAccessToken(accessToken);
        if (principal == null)
        {
            await _auditLogger.LogAsync(AuditEventType.TokenValidationFailed, new AuditEventContext());
            return null;
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            await _auditLogger.LogAsync(AuditEventType.TokenValidationFailed, new AuditEventContext
            {
                UserId = userId
            });
            return null;
        }

        await _auditLogger.LogAsync(AuditEventType.TokenValidated, new AuditEventContext
        {
            UserId = user.Id,
            Email = user.Email
        });

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }
    private async Task HandleFailedLoginAsync(User user, string? ipAddress)
    {
        user.FailedLoginAttempts += 1;

        if (user.FailedLoginAttempts >= _maxFailedLoginAttempts)
        {
            user.LockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
            user.FailedLoginAttempts = 0;
            await _auditLogger.LogAsync(AuditEventType.AccountLocked, new AuditEventContext
            {
                UserId = user.Id,
                Email = user.Email,
                IpAddress = ipAddress
            });
        }

        await _userRepository.UpdateAsync(user);
    }
}
