namespace AuthService.Models;

public static class RefreshTokenRevocationReasons
{
    public const string Replaced = "Replaced";
    public const string Reused = "Reused";
    public const string ManuallyRevoked = "ManuallyRevoked";
    public const string UserDeactivated = "UserDeactivated";
}

