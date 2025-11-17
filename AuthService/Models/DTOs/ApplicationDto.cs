using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Statistics
    public int UserCount { get; set; }
    public int ApiKeyCount { get; set; }
    public int ActiveApiKeyCount { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class CreateApplicationRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Code can only contain letters, numbers, underscores, and hyphens")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }
}
