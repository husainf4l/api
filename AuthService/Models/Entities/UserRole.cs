using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Models.Entities;

public class UserRole
{
    [Key]
    [Column(Order = 0)]
    public Guid UserId { get; set; }

    [Key]
    [Column(Order = 1)]
    public Guid RoleId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;
}
