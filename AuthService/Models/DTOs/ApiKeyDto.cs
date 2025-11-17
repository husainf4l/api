using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class ApiKeyDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string Environment { get; set; } = "development";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    // Related data
    public string ApplicationName { get; set; } = string.Empty;
    public string OwnerUserEmail { get; set; } = string.Empty;
}

public class CreateApiKeyRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public List<string> Scopes { get; set; } = new();

    [Required]
    public string Environment { get; set; } = "development";

    public int? ExpiresInDays { get; set; }

    public int? RateLimitPerHour { get; set; }
}

public class ValidateApiKeyRequest
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string? RequestedScope { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class ValidateApiKeyResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? UserId { get; set; }
    public List<string>? Scopes { get; set; }
}
