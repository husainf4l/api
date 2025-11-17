using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>
    /// Optional: Role name to assign to the user during registration.
    /// If not provided, will attempt to assign default "User" role if it exists.
    /// Each user can only have ONE role.
    /// </summary>
    public string? Role { get; set; }
}
