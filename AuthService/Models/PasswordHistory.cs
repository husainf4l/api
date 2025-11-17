namespace AuthService.Models;

public class PasswordHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

