namespace AuthService.Models;

public class Application
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; } // Hashed
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedScopes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
