using AuthService.Data;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class RoleService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(AuthDbContext context, ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Role?> CreateRoleAsync(Guid applicationId, string name, string? description = null)
    {
        try
        {
            // Check if role already exists for this application
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.Name.ToLower() == name.ToLower());

            if (existingRole != null)
            {
                _logger.LogWarning("Role {RoleName} already exists for application {ApplicationId}", name, applicationId);
                return null;
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Name = name,
                Description = description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleName} created for application {ApplicationId}", name, applicationId);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName} for application {ApplicationId}", name, applicationId);
            return null;
        }
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        try
        {
            // Check if assignment already exists
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingAssignment != null)
            {
                _logger.LogWarning("User {UserId} already has role {RoleId}", userId, roleId);
                return true; // Already assigned, consider it success
            }

            // Verify user and role belong to the same application
            var user = await _context.Users
                .Include(u => u.Application)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (user == null || role == null || user.ApplicationId != role.ApplicationId)
            {
                _logger.LogWarning("Invalid user-role assignment: user {UserId}, role {RoleId}", userId, roleId);
                return false;
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                _logger.LogWarning("User {UserId} does not have role {RoleId}", userId, roleId);
                return false;
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<List<Role>> GetRolesByApplicationAsync(Guid applicationId)
    {
        try
        {
            return await _context.Roles
                .Where(r => r.ApplicationId == applicationId)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for application {ApplicationId}", applicationId);
            return new List<Role>();
        }
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<List<User>> GetUsersByRoleAsync(Guid roleId)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Include(ur => ur.User)
                .Select(ur => ur.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for role {RoleId}", roleId);
            return new List<User>();
        }
    }

    public async Task<bool> CheckPermissionAsync(Guid userId, string roleName, Guid applicationId)
    {
        try
        {
            var hasRole = await _context.UserRoles
                .AnyAsync(ur =>
                    ur.UserId == userId &&
                    ur.Role.Name.ToLower() == roleName.ToLower() &&
                    ur.Role.ApplicationId == applicationId);

            return hasRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}, role {RoleName}, app {ApplicationId}",
                userId, roleName, applicationId);
            return false;
        }
    }

    public async Task<Role?> GetRoleByIdAsync(Guid roleId)
    {
        try
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role {RoleId}", roleId);
            return null;
        }
    }

    public async Task<Role?> GetRoleByNameAsync(Guid applicationId, string roleName)
    {
        try
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.Name.ToLower() == roleName.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role {RoleName} for application {ApplicationId}", roleName, applicationId);
            return null;
        }
    }

    public async Task<bool> UpdateRoleAsync(Guid roleId, string name, string? description)
    {
        try
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                return false;
            }

            // Check if new name conflicts with existing role in same application
            if (role.Name.ToLower() != name.ToLower())
            {
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.ApplicationId == role.ApplicationId && r.Name.ToLower() == name.ToLower() && r.Id != roleId);

                if (existingRole != null)
                {
                    _logger.LogWarning("Role name {RoleName} already exists for application {ApplicationId}", name, role.ApplicationId);
                    return false;
                }
            }

            role.Name = name;
            role.Description = description;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} updated", roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return false;
        }
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                return false;
            }

            // Remove all user-role assignments
            _context.UserRoles.RemoveRange(role.UserRoles);

            // Remove the role
            _context.Roles.Remove(role);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} deleted", roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return false;
        }
    }
}
