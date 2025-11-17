using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}
