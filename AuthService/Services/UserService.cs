using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class UserService
{
    private readonly AuthDbContext _context;
    private readonly RoleService _roleService;
    private readonly ILogger<UserService> _logger;

    public UserService(AuthDbContext context, RoleService roleService, ILogger<UserService> logger)
    {
        _context = context;
        _roleService = roleService;
        _logger = logger;
    }

    public async Task<List<User>> GetUsersByApplicationAsync(Guid applicationId, int page = 1, int pageSize = 50)
    {
        try
        {
            return await _context.Users
                .Where(u => u.ApplicationId == applicationId)
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for application {ApplicationId}", applicationId);
            return new List<User>();
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Application)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByEmailAsync(Guid applicationId, string email)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Application)
                .FirstOrDefaultAsync(u => u.ApplicationId == applicationId && u.NormalizedEmail == email.ToUpper());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email} for application {ApplicationId}", email, applicationId);
            return null;
        }
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            // Update fields
            user.PhoneNumber = request.PhoneNumber;
            user.IsEmailVerified = request.IsEmailVerified ?? user.IsEmailVerified;
            user.IsActive = request.IsActive ?? user.IsActive;

            // Check email change
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                // Check if new email is already taken in the same application
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.ApplicationId == user.ApplicationId &&
                                            u.NormalizedEmail == request.Email.ToUpper() &&
                                            u.Id != userId);

                if (existingUser != null)
                {
                    _logger.LogWarning("Email {Email} already exists for application {ApplicationId}",
                        request.Email, user.ApplicationId);
                    return null;
                }

                user.Email = request.Email;
                user.NormalizedEmail = request.Email.ToUpper();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated", userId);

            return await GetUserDtoAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.RefreshTokens)
                .Include(u => u.SessionLogs)
                .Include(u => u.ApiKeys)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Remove related entities
            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.RefreshTokens.RemoveRange(user.RefreshTokens);
            _context.SessionLogs.RemoveRange(user.SessionLogs);
            _context.ApiKeys.RemoveRange(user.ApiKeys);

            // Remove user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        return await _roleService.AssignRoleToUserAsync(userId, roleId);
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        return await _roleService.RemoveRoleFromUserAsync(userId, roleId);
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        return await _roleService.GetUserRolesAsync(userId);
    }

    public async Task<List<UserDto>> GetUsersWithRolesAsync(Guid applicationId, int page = 1, int pageSize = 50)
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.ApplicationId == applicationId)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IsEmailVerified = user.IsEmailVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
                });
            }

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with roles for application {ApplicationId}", applicationId);
            return new List<UserDto>();
        }
    }

    public async Task<UserDto?> GetUserDtoAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailVerified = user.IsEmailVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user DTO for {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // In a real implementation, you'd validate the current password
            // For now, we'll just update it
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetTotalUsersCountAsync(Guid applicationId)
    {
        try
        {
            return await _context.Users.CountAsync(u => u.ApplicationId == applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total users count for application {ApplicationId}", applicationId);
            return 0;
        }
    }

    public async Task<int> GetActiveUsersCountAsync(Guid applicationId)
    {
        try
        {
            return await _context.Users.CountAsync(u => u.ApplicationId == applicationId && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users count for application {ApplicationId}", applicationId);
            return 0;
        }
    }

    public async Task<List<User>> SearchUsersAsync(Guid applicationId, string searchTerm, int page = 1, int pageSize = 50)
    {
        try
        {
            return await _context.Users
                .Where(u => u.ApplicationId == applicationId &&
                           (u.Email.Contains(searchTerm) ||
                            (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))))
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users for application {ApplicationId} with term {SearchTerm}",
                applicationId, searchTerm);
            return new List<User>();
        }
    }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsEmailVerified { get; set; }
    public bool? IsActive { get; set; }
}
