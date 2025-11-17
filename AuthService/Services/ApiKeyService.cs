using System.Security.Cryptography;
using System.Text;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services;

public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key (Stripe-style format)
    /// Returns: (apiKey object, plainTextKey) - plainTextKey shown only once!
    /// </summary>
    Task<(ApiKey apiKey, string plainTextKey)> GenerateApiKeyAsync(
        Guid userId, 
        string name, 
        List<string> scopes, 
        string? description = null,
        Guid? applicationId = null,
        DateTime? expiresAt = null,
        string environment = "production",
        int? rateLimitPerHour = null,
        int? rateLimitPerDay = null
    );
    
    /// <summary>
    /// Validates an API key and returns the key object if valid
    /// </summary>
    Task<ApiKey?> ValidateApiKeyAsync(string plainTextKey);
    
    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task<bool> RevokeApiKeyAsync(Guid keyId, string reason);
    
    /// <summary>
    /// Updates last used timestamp
    /// </summary>
    Task UpdateLastUsedAsync(Guid keyId, string ipAddress);
    
    /// <summary>
    /// Checks if a key has exceeded rate limits
    /// </summary>
    Task<bool> CheckRateLimitAsync(Guid keyId);
}

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(IApiKeyRepository apiKeyRepository, ILogger<ApiKeyService> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _logger = logger;
    }

    public async Task<(ApiKey apiKey, string plainTextKey)> GenerateApiKeyAsync(
        Guid userId,
        string name,
        List<string> scopes,
        string? description = null,
        Guid? applicationId = null,
        DateTime? expiresAt = null,
        string environment = "production",
        int? rateLimitPerHour = null,
        int? rateLimitPerDay = null)
    {
        // Generate Stripe-style API key
        // Format: ak_{env}_{random32chars}
        // Example: ak_live_3Kj9sL2pQ7mN4xR8tY1vZ5wB6cD0fG2h
        
        var envPrefix = environment.ToLower() switch
        {
            "production" => "live",
            "development" => "test",
            "staging" => "stage",
            _ => "test"
        };

        var randomPart = GenerateRandomString(32);
        var plainTextKey = $"ak_{envPrefix}_{randomPart}";
        
        // Hash the key for storage (SHA-256)
        var keyHash = HashApiKey(plainTextKey);
        
        // Extract prefix for display (first 12 chars)
        var keyPrefix = plainTextKey.Substring(0, Math.Min(12, plainTextKey.Length));

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = applicationId,
            Name = name,
            Description = description,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            Scopes = scopes,
            Environment = environment,
            ExpiresAt = expiresAt,
            RateLimitPerHour = rateLimitPerHour,
            RateLimitPerDay = rateLimitPerDay,
            IsActive = true,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _apiKeyRepository.CreateAsync(apiKey);

        _logger.LogInformation("API key created: {KeyId} for user {UserId}", apiKey.Id, userId);

        // Return both the stored object and the plain text key
        // IMPORTANT: Plain text key is shown only this one time!
        return (apiKey, plainTextKey);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string plainTextKey)
    {
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return null;

        // Hash the provided key
        var keyHash = HashApiKey(plainTextKey);

        // Look up by hash
        var apiKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash);

        if (apiKey == null)
        {
            _logger.LogWarning("API key validation failed: Key not found");
            return null;
        }

        // Check if expired
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("API key validation failed: Key expired {KeyId}", apiKey.Id);
            return null;
        }

        // Check if active and not revoked
        if (!apiKey.IsActive || apiKey.IsRevoked)
        {
            _logger.LogWarning("API key validation failed: Key inactive or revoked {KeyId}", apiKey.Id);
            return null;
        }

        _logger.LogInformation("API key validated successfully: {KeyId}", apiKey.Id);
        return apiKey;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid keyId, string reason)
    {
        var result = await _apiKeyRepository.RevokeAsync(keyId, reason);
        
        if (result)
        {
            _logger.LogInformation("API key revoked: {KeyId}, Reason: {Reason}", keyId, reason);
        }
        
        return result;
    }

    public async Task UpdateLastUsedAsync(Guid keyId, string ipAddress)
    {
        await _apiKeyRepository.UpdateLastUsedAsync(keyId, ipAddress);
    }

    public async Task<bool> CheckRateLimitAsync(Guid keyId)
    {
        // TODO: Implement rate limiting logic
        // For now, always return true (allowed)
        // In production, check Redis or in-memory cache for request counts
        return true;
    }

    /// <summary>
    /// Generates a cryptographically secure random string
    /// </summary>
    private string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[length];
        rng.GetBytes(randomBytes);
        
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[randomBytes[i] % chars.Length];
        }
        
        return new string(result);
    }

    /// <summary>
    /// Hashes an API key using SHA-256
    /// </summary>
    private string HashApiKey(string plainTextKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainTextKey));
        return Convert.ToBase64String(hashBytes);
    }
}
