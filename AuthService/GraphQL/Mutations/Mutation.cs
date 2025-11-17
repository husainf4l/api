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
    // Authentication Mutations (public - no auth required)
    public async Task<TokenResponse> RegisterUser(
        RegisterRequest input,
        [Service] AuthService.Services.AuthService authService,
        [Service] ApplicationService applicationService)
    {
        var result = await authService.RegisterUserAsync(input);
        return result;
    }

    public async Task<TokenResponse> LoginUser(
        LoginRequest input,
        [Service] AuthService.Services.AuthService authService)
    {
        var result = await authService.LoginUserAsync(input);
        return result;
    }

    public async Task<TokenResponse> RefreshToken(
        string refreshToken,
        [Service] JwtTokenService jwtService,
        [Service] AuthDbContext dbContext)
    {
        var result = await jwtService.RefreshTokenAsync(refreshToken);
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
    public async Task<TokenResponse.UserInfo> ChangePassword(
        string currentPassword,
        string newPassword,
        string applicationCode,
        ClaimsPrincipal claimsPrincipal,
        [Service] AuthService.Services.AuthService authService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var result = await authService.ChangePasswordAsync(userId, currentPassword, newPassword, applicationCode);
        return result;
    }

    [Authorize]
    public async Task<bool> ForgotPassword(
        string email,
        string applicationCode,
        [Service] AuthService.Services.AuthService authService)
    {
        await authService.ForgotPasswordAsync(email, applicationCode);
        return true;
    }

    [Authorize]
    public async Task<bool> ResetPassword(
        string email,
        string token,
        string newPassword,
        [Service] AuthService.Services.AuthService authService)
    {
        await authService.ResetPasswordAsync(email, token, newPassword);
        return true;
    }

    // Two-Factor Authentication
    [Authorize]
    public async Task<TwoFactorSetupResponse> SetupTwoFactor(
        ClaimsPrincipal claimsPrincipal,
        [Service] ITwoFactorService twoFactorService,
        [Service] AuthDbContext dbContext)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new GraphQLException("User not found");
        }

        var result = await twoFactorService.SetupTwoFactorAsync(user);
        return result;
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

    // Email Verification
    [Authorize]
    public async Task<bool> VerifyEmail(
        string email,
        string token,
        [Service] AuthService.Services.AuthService authService)
    {
        await authService.VerifyEmailAsync(email, token);
        return true;
    }

    [Authorize]
    public async Task<bool> ResendVerificationEmail(
        string email,
        string applicationCode,
        [Service] AuthService.Services.AuthService authService)
    {
        await authService.ResendVerificationEmailAsync(email, applicationCode);
        return true;
    }

    // Application Management
    [Authorize]
    public async Task<Application> CreateApplication(
        CreateApplicationRequest input,
        [Service] ApplicationService applicationService)
    {
        var result = await applicationService.CreateApplicationAsync(input.Name, input.Code, input.Description);
        return result;
    }

    [Authorize]
    public async Task<Application> UpdateApplication(
        Guid id,
        string? name,
        string? description,
        bool? isActive,
        [Service] ApplicationService applicationService)
    {
        var result = await applicationService.UpdateApplicationAsync(id, name, description, isActive);
        return result;
    }

    [Authorize]
    public async Task<bool> DeleteApplication(
        Guid id,
        [Service] ApplicationService applicationService)
    {
        await applicationService.DeleteApplicationAsync(id);
        return true;
    }

    // User Management
    [Authorize]
    public async Task<User> UpdateUser(
        Guid id,
        string? email,
        string? phoneNumber,
        bool? isActive,
        bool? isEmailVerified,
        [Service] UserService userService)
    {
        var result = await userService.UpdateUserAsync(id, email, phoneNumber, isActive, isEmailVerified);
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
    public async Task<Role> CreateRole(
        Guid applicationId,
        string name,
        string? description,
        [Service] RoleService roleService)
    {
        var result = await roleService.CreateRoleAsync(applicationId, name, description);
        return result;
    }

    [Authorize]
    public async Task<Role> UpdateRole(
        Guid id,
        string? name,
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
        await roleService.DeleteRoleAsync(id);
        return true;
    }

    [Authorize]
    public async Task<bool> AssignRoleToUser(
        Guid userId,
        Guid roleId,
        [Service] RoleService roleService)
    {
        await roleService.AssignRoleToUserAsync(userId, roleId);
        return true;
    }

    [Authorize]
    public async Task<bool> RemoveRoleFromUser(
        Guid userId,
        Guid roleId,
        [Service] RoleService roleService)
    {
        await roleService.RemoveRoleFromUserAsync(userId, roleId);
        return true;
    }

    // API Key Management
    [Authorize]
    public async Task<CreateApiKeyResponse> CreateApiKey(
        Guid applicationId,
        CreateApiKeyRequest input,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApiKeyService apiKeyService)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user");
        }

        var result = await apiKeyService.CreateApiKeyAsync(
            applicationId,
            userId,
            input.Name,
            input.Scopes,
            input.ExpiresInDays);

        return new CreateApiKeyResponse
        {
            Id = result.Id,
            Name = result.Name,
            ApiKey = result.ApiKey,
            Scopes = result.Scopes,
            ExpiresAt = result.ExpiresAt,
            CreatedAt = result.CreatedAt
        };
    }

    [Authorize]
    public async Task<ApiKey> UpdateApiKey(
        Guid id,
        string? name,
        List<string>? scopes,
        [Service] ApiKeyService apiKeyService)
    {
        var result = await apiKeyService.UpdateApiKeyAsync(id, name, scopes);
        return result;
    }

    [Authorize]
    public async Task<bool> RevokeApiKey(
        Guid id,
        [Service] ApiKeyService apiKeyService)
    {
        await apiKeyService.RevokeApiKeyAsync(id);
        return true;
    }

    // Validate API Key (for internal use, typically not exposed via GraphQL)
    public async Task<ValidateApiKeyResponse> ValidateApiKey(
        ValidateApiKeyRequest input,
        [Service] ApiKeyService apiKeyService)
    {
        var result = await apiKeyService.ValidateApiKeyAsync(input.ApiKey, input.RequestedScope, input.IpAddress);
        return result;
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
