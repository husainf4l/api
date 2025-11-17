using AuthService.Data;
using AuthService.Models.Entities;
using AuthService.Services;
using AuthService.GraphQL.Queries;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.GraphQL.Queries;

public class Query
{
    // Simple test query to verify GraphQL is working
    public string Hello(string name = "World")
    {
        return $"Hello, {name}!";
    }

    // Applications
    [Authorize]
    [UsePaging(MaxPageSize = 50, DefaultPageSize = 10, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Application> GetApplications([Service] AuthDbContext dbContext)
    {
        return dbContext.Applications.Where(a => a.IsActive);
    }

    [Authorize]
    public async Task<Application?> GetApplicationById(
        Guid id,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    [Authorize]
    public async Task<Application?> GetApplicationByCode(
        string code,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Code == code);
    }

    // Users
    [Authorize]
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Service] AuthDbContext dbContext)
    {
        return dbContext.Users.Include(u => u.Application).Where(u => u.IsActive);
    }

    [Authorize]
    public async Task<User?> GetUserById(
        Guid id,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.Users
            .Include(u => u.Application)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    [Authorize]
    public async Task<User?> GetUserByEmail(
        string email,
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.Users
            .Include(u => u.Application)
            .FirstOrDefaultAsync(u => u.Email == email && u.ApplicationId == applicationId);
    }

    // Get current authenticated user
    [Authorize]
    public async Task<User?> GetCurrentUser(
        ClaimsPrincipal claimsPrincipal,
        [Service] AuthDbContext dbContext)
    {
        var userIdClaim = claimsPrincipal.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        return await dbContext.Users
            .Include(u => u.Application)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    // Roles
    [Authorize]
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Role> GetRoles([Service] AuthDbContext dbContext)
    {
        return dbContext.Roles.Include(r => r.Application);
    }

    [Authorize]
    public async Task<Role?> GetRoleById(
        Guid id,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.Roles
            .Include(r => r.Application)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // API Keys
    [Authorize]
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ApiKey> GetApiKeys([Service] AuthDbContext dbContext)
    {
        return dbContext.ApiKeys
            .Include(ak => ak.Application)
            .Include(ak => ak.OwnerUser)
            .Where(ak => !ak.IsRevoked);
    }

    [Authorize]
    public async Task<ApiKey?> GetApiKeyById(
        Guid id,
        [Service] AuthDbContext dbContext)
    {
        return await dbContext.ApiKeys
            .Include(ak => ak.Application)
            .Include(ak => ak.OwnerUser)
            .FirstOrDefaultAsync(ak => ak.Id == id);
    }

    // Session Logs
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<SessionLog> GetSessionLogs([Service] AuthDbContext dbContext)
    {
        return dbContext.SessionLogs
            .Include(sl => sl.User)
            .Include(sl => sl.Application);
    }

    // Get user roles
    [Authorize]
    public async Task<IQueryable<Role>> GetUserRoles(
        Guid userId,
        [Service] AuthDbContext dbContext)
    {
        var roleIds = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        return dbContext.Roles.Where(r => roleIds.Contains(r.Id));
    }

    // Get application statistics
    [Authorize]
    public async Task<ApplicationStats> GetApplicationStats(
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        var userCount = await dbContext.Users
            .CountAsync(u => u.ApplicationId == applicationId && u.IsActive);

        var apiKeyCount = await dbContext.ApiKeys
            .CountAsync(ak => ak.ApplicationId == applicationId && ak.IsActive);

        var roleCount = await dbContext.Roles
            .CountAsync(r => r.ApplicationId == applicationId);

        var sessionCount = await dbContext.SessionLogs
            .CountAsync(sl => sl.ApplicationId == applicationId);

        return new ApplicationStats
        {
            ApplicationId = applicationId,
            UserCount = userCount,
            ApiKeyCount = apiKeyCount,
            RoleCount = roleCount,
            SessionCount = sessionCount
        };
    }

    // Get users by application
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsersByApplication(
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        return dbContext.Users
            .Include(u => u.Application)
            .Where(u => u.ApplicationId == applicationId);
    }

    // Get roles by application
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Role> GetRolesByApplication(
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        return dbContext.Roles
            .Include(r => r.Application)
            .Where(r => r.ApplicationId == applicationId);
    }

    // Get API keys by application
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ApiKey> GetApiKeysByApplication(
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        return dbContext.ApiKeys
            .Include(ak => ak.Application)
            .Include(ak => ak.OwnerUser)
            .Where(ak => ak.ApplicationId == applicationId);
    }

    // Get session logs by application
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<SessionLog> GetSessionLogsByApplication(
        Guid applicationId,
        [Service] AuthDbContext dbContext)
    {
        return dbContext.SessionLogs
            .Include(sl => sl.User)
            .Include(sl => sl.Application)
            .Where(sl => sl.ApplicationId == applicationId);
    }

    // Get session logs by user
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<SessionLog> GetUserSessionLogs(
        Guid userId,
        [Service] AuthDbContext dbContext)
    {
        return dbContext.SessionLogs
            .Include(sl => sl.User)
            .Include(sl => sl.Application)
            .Where(sl => sl.UserId == userId);
    }
}

// DTO for application statistics
public class ApplicationStats
{
    public Guid ApplicationId { get; set; }
    public int UserCount { get; set; }
    public int ApiKeyCount { get; set; }
    public int RoleCount { get; set; }
    public int SessionCount { get; set; }
}
