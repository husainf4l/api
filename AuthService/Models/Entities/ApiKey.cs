using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Models.Entities;

public class ApiKey
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string HashedKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Scope { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsRevoked => RevokedAt.HasValue;

    // Navigation properties
    [ForeignKey(nameof(ApplicationId))]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey(nameof(OwnerUserId))]
    public virtual User OwnerUser { get; set; } = null!;
}
