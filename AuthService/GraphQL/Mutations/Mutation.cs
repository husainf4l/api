using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using AuthService.Services;
using AuthService.GraphQL.Mutations;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.GraphQL.Mutations;

public class Mutation
{
    // Simple test mutation to verify GraphQL is working
    public string Hello(string name = "World")
    {
        return $"Hello, {name}!";
    }

    // Authentication Mutations (public - no auth required)
    public async Task<TokenResponse?> RegisterUser(
        RegisterRequest input,
        [Service] AuthService.Services.AuthService authService)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(input.Email))
            throw new GraphQLException("Email is required");

        if (string.IsNullOrWhiteSpace(input.Password))
            throw new GraphQLException("Password is required");

        if (input.Password.Length < 8)
            throw new GraphQLException("Password must be at least 8 characters long");

        if (input.Password != input.ConfirmPassword)
            throw new GraphQLException("Password confirmation does not match");

        if (string.IsNullOrWhiteSpace(input.ApplicationCode))
            throw new GraphQLException("Application code is required");

        var result = await authService.RegisterUserAsync(input);

        if (result == null)
            throw new GraphQLException("Registration failed. Please check your email and application code.");

        return result;
    }

    public async Task<TokenResponse?> LoginUser(
        LoginRequest input,
        [Service] AuthService.Services.AuthService authService)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(input.Email))
            throw new GraphQLException("Email is required");

        if (string.IsNullOrWhiteSpace(input.Password))
            throw new GraphQLException("Password is required");

        if (string.IsNullOrWhiteSpace(input.ApplicationCode))
            throw new GraphQLException("Application code is required");

        var result = await authService.LoginUserAsync(input);

        if (result == null)
            throw new GraphQLException("Login failed. Please check your credentials and application code.");

        return result;
    }

    // Authenticated User Mutations
    [Authorize]
    public async Task<bool> LogoutUser(
        string refreshToken,
        [Service] AuthService.Services.AuthService authService)
    {
        await authService.LogoutUserAsync(refreshToken);
        return true;
    }

    [Authorize]
    public async Task<TokenResponse?> RefreshToken(
        string refreshToken,
        [Service] AuthService.Services.AuthService authService)
    {
        var result = await authService.RefreshTokenAsync(refreshToken);
        return result;
    }

    // Application Management
    [Authorize]
    public async Task<ApplicationDto?> CreateApplication(
        string name,
        string code,
        string? description,
        [Service] ApplicationService applicationService)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new GraphQLException("Application name is required");

        if (name.Length < 2 || name.Length > 100)
            throw new GraphQLException("Application name must be between 2 and 100 characters");

        if (string.IsNullOrWhiteSpace(code))
            throw new GraphQLException("Application code is required");

        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[a-z0-9_-]+$"))
            throw new GraphQLException("Application code can only contain lowercase letters, numbers, hyphens, and underscores");

        if (code.Length < 2 || code.Length > 50)
            throw new GraphQLException("Application code must be between 2 and 50 characters");

        var request = new CreateApplicationRequest
        {
            Name = name,
            Code = code.ToLower(),
            Description = description
        };

        var result = await applicationService.CreateApplicationAsync(request);

        if (result == null)
            throw new GraphQLException("Failed to create application. The code may already be in use.");

        return result;
    }

    [Authorize]
    public async Task<ApplicationDto?> UpdateApplication(
        Guid id,
        string? name,
        bool? isActive,
        [Service] ApplicationService applicationService)
    {
        // Input validation
        if (name != null && (name.Length < 2 || name.Length > 100))
            throw new GraphQLException("Application name must be between 2 and 100 characters");

        var result = await applicationService.UpdateApplicationAsync(id, name, isActive);

        if (result == null)
            throw new GraphQLException("Application not found or update failed.");

        return result;
    }

    [Authorize]
    public async Task<bool> DeleteApplication(
        Guid id,
        [Service] ApplicationService applicationService)
    {
        var result = await applicationService.DeleteApplicationAsync(id);
        return result;
    }

    // User Management
    [Authorize]
    public async Task<UserDto?> UpdateUser(
        Guid id,
        string? email,
        string? phoneNumber,
        bool? isActive,
        bool? isEmailVerified,
        [Service] UserService userService)
    {
        var request = new UpdateUserRequest
        {
            Email = email,
            PhoneNumber = phoneNumber,
            IsEmailVerified = isEmailVerified,
            IsActive = isActive
        };
        var result = await userService.UpdateUserAsync(id, request);
        return result;
    }

    [Authorize]
    public async Task<bool> DeleteUser(
        Guid id,
        [Service] UserService userService)
    {
        await userService.DeleteUserAsync(id);
        return true;
    }

    // Role Management
    [Authorize]
    public async Task<Role?> CreateRole(
        Guid applicationId,
        string name,
        string? description,
        [Service] RoleService roleService)
    {
        var result = await roleService.CreateRoleAsync(applicationId, name, description);
        return result;
    }

    [Authorize]
    public async Task<bool> UpdateRole(
        Guid id,
        string name,
        string? description,
        [Service] RoleService roleService)
    {
        var result = await roleService.UpdateRoleAsync(id, name, description);
        return result;
    }

    [Authorize]
    public async Task<bool> DeleteRole(
        Guid id,
        [Service] RoleService roleService)
    {
        var result = await roleService.DeleteRoleAsync(id);
        return result;
    }

    [Authorize]
    public async Task<bool> AssignRoleToUser(
        Guid userId,
        Guid roleId,
        [Service] RoleService roleService)
    {
        var result = await roleService.AssignRoleToUserAsync(userId, roleId);
        return result;
    }

    [Authorize]
    public async Task<bool> RemoveRoleFromUser(
        Guid userId,
        Guid roleId,
        [Service] RoleService roleService)
    {
        var result = await roleService.RemoveRoleFromUserAsync(userId, roleId);
        return result;
    }

    // API Key Management
    [Authorize]
    public async Task<CreateApiKeyResponse> CreateApiKey(
        Guid applicationId,
        string name,
        string? description,
        List<string>? scopes,
        string environment,
        int? expiresInDays,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApiKeyService apiKeyService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var request = new CreateApiKeyRequest
        {
            Name = name,
            Description = description,
            Scopes = scopes ?? new List<string>(),
            Environment = environment,
            ExpiresInDays = expiresInDays
        };

        var result = await apiKeyService.CreateApiKeyAsync(request, applicationId, userId);

        if (result == null)
        {
            throw new GraphQLException("Failed to create API key");
        }

        return new CreateApiKeyResponse
        {
            Id = result.ApiKeyEntity.Id,
            Name = result.ApiKeyEntity.Name,
            ApiKey = result.ApiKey,
            Scopes = result.ApiKeyEntity.Scope.Split(',').ToList(),
            ExpiresAt = result.ApiKeyEntity.ExpiresAt,
            CreatedAt = result.ApiKeyEntity.CreatedAt
        };
    }

    [Authorize]
    public async Task<bool> UpdateApiKey(
        Guid id,
        string? name,
        List<string>? scopes,
        int? expiresInDays,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApiKeyService apiKeyService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var request = new UpdateApiKeyRequest
        {
            Name = name,
            Scopes = scopes,
            ExpiresInDays = expiresInDays
        };

        var result = await apiKeyService.UpdateApiKeyAsync(id, request, userId);
        return result;
    }

    [Authorize]
    public async Task<bool> RevokeApiKey(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApiKeyService apiKeyService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        await apiKeyService.RevokeApiKeyAsync(id, userId);
        return true;
    }

    // Validate API Key (for internal use)
    public async Task<ValidateApiKeyResponse> ValidateApiKey(
        string apiKey,
        string? requestedScope,
        string? ipAddress,
        [Service] ApiKeyService apiKeyService)
    {
        var result = await apiKeyService.ValidateApiKeyAsync(apiKey, requestedScope, ipAddress);
        return result;
    }

    // Two-Factor Authentication
    [Authorize]
    public async Task<TwoFactorSetupResponse> SetupTwoFactor(
        ClaimsPrincipal claimsPrincipal,
        [Service] ITwoFactorService twoFactorService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var result = await twoFactorService.SetupTwoFactorAsync(userId);

        if (!result.Success || result.Error != null)
        {
            throw new GraphQLException(result.Error ?? "Failed to setup 2FA");
        }

        return new TwoFactorSetupResponse
        {
            Success = result.Success,
            Secret = result.Secret ?? string.Empty,
            QrCodeUri = result.QrCodeUri ?? string.Empty,
            QrCodeImageBase64 = result.QrCodeImage != null ? Convert.ToBase64String(result.QrCodeImage) : string.Empty,
            BackupCodes = result.BackupCodes ?? new List<string>()
        };
    }

    [Authorize]
    public async Task<bool> EnableTwoFactor(
        string verificationCode,
        ClaimsPrincipal claimsPrincipal,
        [Service] ITwoFactorService twoFactorService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        await twoFactorService.EnableTwoFactorAsync(userId, verificationCode);
        return true;
    }

    [Authorize]
    public async Task<bool> DisableTwoFactor(
        ClaimsPrincipal claimsPrincipal,
        [Service] ITwoFactorService twoFactorService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        await twoFactorService.DisableTwoFactorAsync(userId);
        return true;
    }

    // Password Management
    [Authorize]
    public async Task<bool> ChangePassword(
        string currentPassword,
        string newPassword,
        ClaimsPrincipal claimsPrincipal,
        [Service] AuthService.Services.AuthService authService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        await authService.ChangePasswordAsync(userId, currentPassword, newPassword);
        return true;
    }

    // Password reset functionality
    public async Task<bool> ForgotPassword(
        string email,
        string applicationCode,
        [Service] AuthService.Services.AuthService authService,
        [Service] ApplicationService applicationService)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new GraphQLException("Email is required");

        if (string.IsNullOrWhiteSpace(applicationCode))
            throw new GraphQLException("Application code is required");

        // Get application by code
        var application = await applicationService.GetApplicationByCodeAsync(applicationCode);
        if (application == null)
            throw new GraphQLException("Invalid application code");

        await authService.RequestPasswordResetAsync(email, application.Id);
        return true;
    }

    public async Task<bool> ResetPassword(
        string email,
        string token,
        string newPassword,
        string confirmPassword,
        [Service] AuthService.Services.AuthService authService)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new GraphQLException("Email is required");

        if (string.IsNullOrWhiteSpace(token))
            throw new GraphQLException("Token is required");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new GraphQLException("New password is required");

        if (newPassword.Length < 8)
            throw new GraphQLException("Password must be at least 8 characters long");

        if (newPassword != confirmPassword)
            throw new GraphQLException("Password confirmation does not match");

        var result = await authService.ResetPasswordAsync(email, token, newPassword);
        
        if (!result)
            throw new GraphQLException("Invalid or expired reset token");

        return true;
    }

    // Email verification functionality
    public async Task<bool> VerifyEmail(
        string email,
        string token,
        [Service] AuthService.Services.AuthService authService)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new GraphQLException("Email is required");

        if (string.IsNullOrWhiteSpace(token))
            throw new GraphQLException("Token is required");

        var result = await authService.VerifyEmailAsync(email, token);
        
        if (!result)
            throw new GraphQLException("Invalid or expired verification token");

        return true;
    }
}

// Response DTOs for mutations
public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string QrCodeImageBase64 { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new();
}

public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
