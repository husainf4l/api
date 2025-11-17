using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    public required string Password { get; set; }
    
    public string? ClientId { get; set; }
    
    public string? DeviceInfo { get; set; }
}
