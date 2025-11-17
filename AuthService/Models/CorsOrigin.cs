namespace AuthService.Models;

public class CorsOrigin
{
    public Guid Id { get; set; }
    public required string Origin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

