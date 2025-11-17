using AuthService.Data;
using AuthService.Models.DTOs;
using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class ApiKeyService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(AuthDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateApiKeyResult?> CreateApiKeyAsync(CreateApiKeyRequest request, Guid applicationId, Guid ownerUserId)
    {
        try
        {
            // Check if API key with same name exists for this application
            var existingKey = await _context.ApiKeys
                .FirstOrDefaultAsync(ak => ak.ApplicationId == applicationId && ak.Name.ToLower() == request.Name.ToLower() && !ak.IsRevoked);

            if (existingKey != null)
            {
                _logger.LogWarning("API key with name {KeyName} already exists for application {ApplicationId}", request.Name, applicationId);
                return null;
            }

            // Generate a secure API key
            var apiKey = GenerateSecureApiKey();
            var hashedKey = HashApiKey(apiKey);

            // Create the API key entity
            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                OwnerUserId = ownerUserId,
                Name = request.Name,
                HashedKey = hashedKey,
                Scope = string.Join(",", request.Scopes),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresInDays.HasValue ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value) : null,
                IsActive = true
            };

            _context.ApiKeys.Add(apiKeyEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API key {KeyName} created for application {ApplicationId}", request.Name, applicationId);

            return new CreateApiKeyResult
            {
                ApiKey = apiKey,
                ApiKeyEntity = apiKeyEntity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key {KeyName} for application {ApplicationId}", request.Name, applicationId);
            return null;
        }
    }

    public async Task<ApiKeyDto?> GetApiKeyByIdAsync(Guid apiKeyId)
    {
        try
        {
            var apiKey = await _context.ApiKeys
                .Include(ak => ak.Application)
                .Include(ak => ak.OwnerUser)
                .FirstOrDefaultAsync(ak => ak.Id == apiKeyId);

            if (apiKey == null) return null;

            return MapToApiKeyDto(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key {ApiKeyId}", apiKeyId);
            return null;
        }
    }

    public async Task<List<ApiKeyDto>> GetApiKeysByApplicationAsync(Guid applicationId, int page = 1, int pageSize = 50)
    {
        try
        {
            var apiKeys = await _context.ApiKeys
                .Include(ak => ak.Application)
                .Include(ak => ak.OwnerUser)
                .Where(ak => ak.ApplicationId == applicationId)
                .OrderByDescending(ak => ak.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return apiKeys.Select(MapToApiKeyDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys for application {ApplicationId}", applicationId);
            return new List<ApiKeyDto>();
        }
    }

    public async Task<List<ApiKeyDto>> GetApiKeysByUserAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        try
        {
            var apiKeys = await _context.ApiKeys
                .Include(ak => ak.Application)
                .Include(ak => ak.OwnerUser)
                .Where(ak => ak.OwnerUserId == userId)
                .OrderByDescending(ak => ak.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return apiKeys.Select(MapToApiKeyDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys for user {UserId}", userId);
            return new List<ApiKeyDto>();
        }
    }

    public async Task<ValidateApiKeyResponse> ValidateApiKeyAsync(string apiKey, string? requestedScope = null, string? clientIp = null)
    {
        try
        {
            // Hash the provided API key
            var hashedKey = HashApiKey(apiKey);

            // Find the API key in database
            var apiKeyEntity = await _context.ApiKeys
                .Include(ak => ak.Application)
                .Include(ak => ak.OwnerUser)
                .FirstOrDefaultAsync(ak => ak.HashedKey == hashedKey);

            if (apiKeyEntity == null)
            {
                return new ValidateApiKeyResponse { IsValid = false, Message = "API key not found" };
            }

            // Check if key is active and not revoked/expired
            if (!apiKeyEntity.IsActive || apiKeyEntity.IsRevoked || apiKeyEntity.ExpiresAt < DateTime.UtcNow)
            {
                var reason = !apiKeyEntity.IsActive ? "API key is inactive" :
                           apiKeyEntity.IsRevoked ? "API key has been revoked" :
                           "API key has expired";
                return new ValidateApiKeyResponse { IsValid = false, Message = reason };
            }

            // Check rate limiting
            var rateLimitResult = await CheckRateLimitAsync(apiKeyEntity.Id, clientIp);
            if (!rateLimitResult.Allowed)
            {
                return new ValidateApiKeyResponse { IsValid = false, Message = $"Rate limit exceeded. Try again in {rateLimitResult.ResetInSeconds} seconds." };
            }

            // Check scope if requested
            if (!string.IsNullOrEmpty(requestedScope))
            {
                var scopes = apiKeyEntity.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!scopes.Contains(requestedScope, StringComparer.OrdinalIgnoreCase))
                {
                    return new ValidateApiKeyResponse { IsValid = false, Message = $"API key does not have required scope: {requestedScope}" };
                }
            }

            // Update last used timestamp
            apiKeyEntity.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Record usage for rate limiting
            await RecordApiKeyUsageAsync(apiKeyEntity.Id, clientIp);

            _logger.LogInformation("API key validated successfully for application {ApplicationId}", apiKeyEntity.ApplicationId);

            return new ValidateApiKeyResponse
            {
                IsValid = true,
                ApplicationId = apiKeyEntity.ApplicationId,
                UserId = apiKeyEntity.OwnerUserId,
                Scopes = apiKeyEntity.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return new ValidateApiKeyResponse { IsValid = false, Message = "Internal error during validation" };
        }
    }

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId, Guid requestingUserId)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null)
            {
                _logger.LogWarning("API key {ApiKeyId} not found", apiKeyId);
                return false;
            }

            // Check if user owns this API key or has admin permissions
            // For now, allow any user to revoke (this should be enhanced with proper permissions)
            if (apiKey.OwnerUserId != requestingUserId)
            {
                _logger.LogWarning("User {UserId} attempted to revoke API key {ApiKeyId} owned by {OwnerId}",
                    requestingUserId, apiKeyId, apiKey.OwnerUserId);
                return false;
            }

            apiKey.RevokedAt = DateTime.UtcNow;
            apiKey.IsActive = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation("API key {ApiKeyId} revoked by user {UserId}", apiKeyId, requestingUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId}", apiKeyId);
            return false;
        }
    }

    public async Task<bool> UpdateApiKeyAsync(Guid apiKeyId, UpdateApiKeyRequest request, Guid requestingUserId)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null)
            {
                _logger.LogWarning("API key {ApiKeyId} not found", apiKeyId);
                return false;
            }

            // Check ownership
            if (apiKey.OwnerUserId != requestingUserId)
            {
                _logger.LogWarning("User {UserId} attempted to update API key {ApiKeyId} owned by {OwnerId}",
                    requestingUserId, apiKeyId, apiKey.OwnerUserId);
                return false;
            }

            // Update fields
            if (!string.IsNullOrEmpty(request.Name))
            {
                apiKey.Name = request.Name;
            }

            if (request.Scopes != null && request.Scopes.Any())
            {
                apiKey.Scope = string.Join(",", request.Scopes);
            }

            if (request.ExpiresInDays.HasValue)
            {
                apiKey.ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiresInDays.Value);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("API key {ApiKeyId} updated by user {UserId}", apiKeyId, requestingUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key {ApiKeyId}", apiKeyId);
            return false;
        }
    }

    public async Task<int> GetApiKeyCountAsync(Guid applicationId)
    {
        try
        {
            return await _context.ApiKeys.CountAsync(ak => ak.ApplicationId == applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key count for application {ApplicationId}", applicationId);
            return 0;
        }
    }

    public async Task<int> GetActiveApiKeyCountAsync(Guid applicationId)
    {
        try
        {
            return await _context.ApiKeys.CountAsync(ak =>
                ak.ApplicationId == applicationId &&
                ak.IsActive &&
                !ak.IsRevoked &&
                (ak.ExpiresAt == null || ak.ExpiresAt > DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active API key count for application {ApplicationId}", applicationId);
            return 0;
        }
    }

    private string GenerateSecureApiKey()
    {
        // Generate a secure random API key
        // Format: prefix + random string (e.g., ak_live_abc123def456...)
        const string prefix = "ak_live_";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var key = new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        return prefix + key;
    }

    // Rate limiting storage (in production, use Redis or distributed cache)
    private static readonly Dictionary<string, List<DateTime>> _rateLimitCache = new();
    private static readonly object _rateLimitLock = new();

    private async Task<RateLimitResult> CheckRateLimitAsync(Guid apiKeyId, string? clientIp)
    {
        // Simple in-memory rate limiting (100 requests per minute per API key)
        const int maxRequestsPerMinute = 100;
        var cacheKey = $"{apiKeyId}:{clientIp ?? "unknown"}";

        lock (_rateLimitLock)
        {
            if (!_rateLimitCache.ContainsKey(cacheKey))
            {
                _rateLimitCache[cacheKey] = new List<DateTime>();
            }

            var requests = _rateLimitCache[cacheKey];
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Remove requests older than 1 minute
            requests.RemoveAll(r => r < oneMinuteAgo);

            if (requests.Count >= maxRequestsPerMinute)
            {
                var oldestRequest = requests.Min();
                var resetTime = oldestRequest.AddMinutes(1);
                var resetInSeconds = (int)(resetTime - now).TotalSeconds;

                return new RateLimitResult { Allowed = false, ResetInSeconds = Math.Max(1, resetInSeconds) };
            }

            requests.Add(now);
            return new RateLimitResult { Allowed = true, ResetInSeconds = 60 };
        }
    }

    private async Task RecordApiKeyUsageAsync(Guid apiKeyId, string? clientIp)
    {
        // In a production system, you would store this in a database or time-series database
        // For now, we'll just log it
        _logger.LogInformation("API key {ApiKeyId} used from IP {ClientIp}", apiKeyId, clientIp ?? "unknown");
    }

    private string HashApiKey(string apiKey)
    {
        // Use SHA-256 to hash the API key for storage
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private ApiKeyDto MapToApiKeyDto(ApiKey apiKey)
    {
        return new ApiKeyDto
        {
            Id = apiKey.Id,
            ApplicationId = apiKey.ApplicationId,
            OwnerUserId = apiKey.OwnerUserId,
            Name = apiKey.Name,
            Scopes = apiKey.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            IsActive = apiKey.IsActive,
            IsRevoked = apiKey.IsRevoked,
            ApplicationName = apiKey.Application.Name,
            OwnerUserEmail = apiKey.OwnerUser.Email
        };
    }
}

public class CreateApiKeyResult
{
    public string ApiKey { get; set; } = string.Empty;
    public ApiKey ApiKeyEntity { get; set; } = null!;
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public List<string>? Scopes { get; set; }
    public int? ExpiresInDays { get; set; }
}

public class RateLimitResult
{
    public bool Allowed { get; set; }
    public int ResetInSeconds { get; set; }
}
