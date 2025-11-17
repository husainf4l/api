using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class ApplicationService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(AuthDbContext context, ILogger<ApplicationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Application?> GetApplicationByCodeAsync(string code)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.Code.ToLower() == code.ToLower() && a.IsActive);
    }

    public async Task<Application?> GetApplicationByIdAsync(Guid id)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
    }

    public async Task<ApplicationDto?> CreateApplicationAsync(CreateApplicationRequest request)
    {
        try
        {
            // Check if code already exists
            var existingApp = await _context.Applications
                .FirstOrDefaultAsync(a => a.Code.ToLower() == request.Code.ToLower());

            if (existingApp != null)
            {
                _logger.LogWarning("Attempt to create application with existing code: {Code}", request.Code);
                return null;
            }

            // Generate client credentials
            var clientId = GenerateClientId();
            var clientSecret = GenerateClientSecret();

            var application = new Application
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code.ToLower(),
                ClientId = clientId,
                ClientSecretHash = HashClientSecret(clientSecret),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Applications.Add(application);

            // Create default roles for the application
            var defaultRoles = new[]
            {
                new Role { Id = Guid.NewGuid(), ApplicationId = application.Id, Name = "admin", Description = "Administrator with full access" },
                new Role { Id = Guid.NewGuid(), ApplicationId = application.Id, Name = "user", Description = "Standard user role" },
                new Role { Id = Guid.NewGuid(), ApplicationId = application.Id, Name = "moderator", Description = "Content moderator role" }
            };

            foreach (var role in defaultRoles)
            {
                _context.Roles.Add(role);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Application created successfully: {Name} ({Code})", application.Name, application.Code);

            return new ApplicationDto
            {
                Id = application.Id,
                Name = application.Name,
                Code = application.Code,
                CreatedAt = application.CreatedAt,
                IsActive = application.IsActive,
                UserCount = 0,
                ApiKeyCount = 0,
                ActiveApiKeyCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application: {Name}", request.Name);
            return null;
        }
    }

    public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
    {
        var applications = await _context.Applications
            .Where(a => a.IsActive)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                CreatedAt = a.CreatedAt,
                IsActive = a.IsActive,
                UserCount = a.Users.Count(u => u.IsActive),
                ApiKeyCount = a.ApiKeys.Count(),
                ActiveApiKeyCount = a.ApiKeys.Count(ak => ak.IsActive && !ak.IsRevoked && (ak.ExpiresAt == null || ak.ExpiresAt > DateTime.UtcNow)),
                LastActivity = a.Users.Where(u => u.LastLoginAt.HasValue).Max(u => u.LastLoginAt)
            })
            .ToListAsync();

        return applications;
    }

    public async Task<ApplicationDto?> UpdateApplicationAsync(Guid applicationId, string? name, bool? isActive)
    {
        try
        {
            var application = await _context.Applications.FindAsync(applicationId);

            if (application == null)
            {
                _logger.LogWarning("Attempt to update non-existent application: {ApplicationId}", applicationId);
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(name))
            {
                application.Name = name;
            }

            if (isActive.HasValue)
            {
                application.IsActive = isActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Application updated successfully: {ApplicationId}", applicationId);

            // Return updated application
            var applications = await GetAllApplicationsAsync();
            return applications.FirstOrDefault(a => a.Id == applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application: {ApplicationId}", applicationId);
            return null;
        }
    }

    public async Task<bool> DeleteApplicationAsync(Guid applicationId)
    {
        try
        {
            var application = await _context.Applications.FindAsync(applicationId);

            if (application == null)
            {
                _logger.LogWarning("Attempt to delete non-existent application: {ApplicationId}", applicationId);
                return false;
            }

            // Soft delete - mark as inactive instead of hard delete
            application.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Application deleted successfully: {ApplicationId}", applicationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application: {ApplicationId}", applicationId);
            return false;
        }
    }

    public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.ClientId == clientId && a.IsActive);

        if (application == null)
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(clientSecret, application.ClientSecretHash);
    }

    private string GenerateClientId()
    {
        return $"app_{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    private string GenerateClientSecret()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string HashClientSecret(string secret)
    {
        return BCrypt.Net.BCrypt.HashPassword(secret);
    }
}
