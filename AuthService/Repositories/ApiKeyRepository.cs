using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    Task<List<ApiKey>> GetByUserIdAsync(Guid userId, bool includeRevoked = false);
    Task<List<ApiKey>> GetByApplicationIdAsync(Guid applicationId, bool includeRevoked = false);
    Task<List<ApiKey>> GetAllAsync(bool includeRevoked = false);
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
    Task<bool> RevokeAsync(Guid id, string reason);
    Task<bool> DeleteAsync(Guid id);
    Task UpdateLastUsedAsync(Guid id, string ipAddress);
    Task<int> CountActiveByUserAsync(Guid userId);
    Task<List<ApiKey>> GetExpiringKeysAsync(int daysBeforeExpiry);
}

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AuthDbContext _context;

    public ApiKeyRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .Include(k => k.Application)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .Include(k => k.Application)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive && !k.IsRevoked);
    }

    public async Task<List<ApiKey>> GetByUserIdAsync(Guid userId, bool includeRevoked = false)
    {
        var query = _context.ApiKeys
            .Include(k => k.Application)
            .Where(k => k.UserId == userId);

        if (!includeRevoked)
        {
            query = query.Where(k => !k.IsRevoked);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ApiKey>> GetByApplicationIdAsync(Guid applicationId, bool includeRevoked = false)
    {
        var query = _context.ApiKeys
            .Include(k => k.User)
            .Where(k => k.ApplicationId == applicationId);

        if (!includeRevoked)
        {
            query = query.Where(k => !k.IsRevoked);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ApiKey>> GetAllAsync(bool includeRevoked = false)
    {
        var query = _context.ApiKeys
            .Include(k => k.User)
            .Include(k => k.Application)
            .AsQueryable();

        if (!includeRevoked)
        {
            query = query.Where(k => !k.IsRevoked);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        apiKey.CreatedAt = DateTime.UtcNow;
        apiKey.UpdatedAt = DateTime.UtcNow;
        
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        
        return apiKey;
    }

    public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
    {
        apiKey.UpdatedAt = DateTime.UtcNow;
        
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync();
        
        return apiKey;
    }

    public async Task<bool> RevokeAsync(Guid id, string reason)
    {
        var apiKey = await GetByIdAsync(id);
        if (apiKey == null) return false;

        apiKey.IsRevoked = true;
        apiKey.IsActive = false;
        apiKey.RevokedAt = DateTime.UtcNow;
        apiKey.RevokedReason = reason;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var apiKey = await GetByIdAsync(id);
        if (apiKey == null) return false;

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateLastUsedAsync(Guid id, string ipAddress)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey == null) return;

        apiKey.LastUsedAt = DateTime.UtcNow;
        apiKey.LastUsedIp = ipAddress;
        apiKey.UsageCount++;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<int> CountActiveByUserAsync(Guid userId)
    {
        return await _context.ApiKeys
            .CountAsync(k => k.UserId == userId && k.IsActive && !k.IsRevoked);
    }

    public async Task<List<ApiKey>> GetExpiringKeysAsync(int daysBeforeExpiry)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysBeforeExpiry);
        
        return await _context.ApiKeys
            .Include(k => k.User)
            .Include(k => k.Application)
            .Where(k => k.IsActive && 
                       !k.IsRevoked && 
                       k.ExpiresAt != null && 
                       k.ExpiresAt <= expiryThreshold)
            .ToListAsync();
    }
}
