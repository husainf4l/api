namespace AuthService.DTOs;

public class AuthResponse
{
    public required TokenResponse Tokens { get; set; }
    public required UserResponse User { get; set; }
}
