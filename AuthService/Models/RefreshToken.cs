using System;

namespace AuthService.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ApplicationId { get; set; }
    [Obsolete("Use TokenHash to persist refresh tokens securely.")]
    public string? Token { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }
    public string? CreatedByIp { get; set; }
    public string? DeviceInfo { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Application? Application { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
