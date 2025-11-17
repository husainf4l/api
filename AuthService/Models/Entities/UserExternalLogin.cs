using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Models.Entities;

public class UserExternalLogin
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // "Google", "GitHub", "Microsoft", etc.

    [Required]
    [MaxLength(100)]
    public string ProviderUserId { get; set; } = string.Empty; // User's ID from the OAuth provider

    [MaxLength(256)]
    public string? ProviderUserEmail { get; set; }

    [MaxLength(100)]
    public string? ProviderUserName { get; set; }

    [MaxLength(500)]
    public string? AccessToken { get; set; } // Optional: store access token

    [MaxLength(500)]
    public string? RefreshToken { get; set; } // Optional: store refresh token

    public DateTime? TokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    // Composite unique constraint to prevent duplicate provider-user combinations
    // Note: Index is configured in AuthDbContext using fluent API
}
