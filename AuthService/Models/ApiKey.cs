namespace AuthService.Models;

public class ApiKey
{
    public Guid Id { get; set; }
    
    // The display prefix (e.g., "ak_live_" or "ak_test_")
    public string KeyPrefix { get; set; } = string.Empty;
    
    // SHA-256 hashed full key (never store plain text)
    public string KeyHash { get; set; } = string.Empty;
    
    // Owner of the API key
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Optional: Associate key with specific application
    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }
    
    // Name/description for identification
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Scopes define what this key can access
    // Examples: "email-service", "sms-service", "crm-service", "admin", "read-only"
    public List<string> Scopes { get; set; } = new();
    
    // Rate limiting per key
    public int? RateLimitPerHour { get; set; }
    public int? RateLimitPerDay { get; set; }
    
    // Expiration
    public DateTime? ExpiresAt { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    
    // Usage tracking
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public int UsageCount { get; set; } = 0;
    
    // Metadata
    public string? Environment { get; set; } // "production", "development", "staging"
    public Dictionary<string, string>? Metadata { get; set; } // Additional custom data
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
