using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    [MinLength(8)]
    public required string Password { get; set; }
    
    [Required]
    [MinLength(2)]
    public required string FirstName { get; set; }
    
    [Required]
    [MinLength(2)]
    public required string LastName { get; set; }
    
    public string? ClientId { get; set; }
}
