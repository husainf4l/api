using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Models.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; } = false;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool TwoFactorEnabled { get; set; } = false;

    [MaxLength(256)]
    public string? TwoFactorSecret { get; set; }

    [MaxLength(100)]
    public string? TwoFactorBackupCodes { get; set; } // JSON array of backup codes

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(ApplicationId))]
    public virtual Application Application { get; set; } = null!;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<SessionLog> SessionLogs { get; set; } = new List<SessionLog>();
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
