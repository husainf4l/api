namespace AuthService.DTOs;

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Scopes { get; set; } = new();
    public Guid? ApplicationId { get; set; }
    public int? ExpiresInDays { get; set; }
    public string Environment { get; set; } = "production";
    public int? RateLimitPerHour { get; set; }
    public int? RateLimitPerDay { get; set; }
}

public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    
    // CRITICAL: This is shown only once!
    public string ApiKey { get; set; } = string.Empty;
    public string Warning { get; set; } = "Store this API key securely. It will not be shown again.";
    
    public List<string> Scopes { get; set; } = new();
    public string Environment { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string? ApplicationName { get; set; }
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ValidateApiKeyRequest
{
    public string ApiKey { get; set; } = string.Empty;
}

public class ValidateApiKeyResponse
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string? Message { get; set; }
}

public class RevokeApiKeyRequest
{
    public string Reason { get; set; } = "Revoked by user";
}
