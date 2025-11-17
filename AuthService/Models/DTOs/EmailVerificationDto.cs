using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.DTOs;

public class EmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class PasswordResetRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    [MinLength(6)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class EmailTokenDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty; // "email_verification" or "password_reset"
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
}

public class TwoFactorSetupRequest
{
    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class TwoFactorEnableRequest
{
    [Required]
    public string ApplicationCode { get; set; } = string.Empty;

    [Required]
    public string VerificationCode { get; set; } = string.Empty;
}

public class TwoFactorVerifyRequest
{
    [Required]
    public string ApplicationCode { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty; // TOTP code or backup code
}

public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Secret { get; set; }
    public string? QrCodeUri { get; set; }
    public string? QrCodeImageBase64 { get; set; }
    public List<string>? BackupCodes { get; set; }
}

public class TwoFactorStatusResponse
{
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
    public bool HasBackupCodes { get; set; }
}

public class ExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // "Google", "GitHub", etc.

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class ExternalLoginCallbackRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;

    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string? Code { get; set; }
    public string? State { get; set; }
}

public class LinkExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class UnlinkExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string ApplicationCode { get; set; } = string.Empty;
}

public class ExternalLoginInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? ProviderUserEmail { get; set; }
    public string? ProviderUserName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExternalLoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TokenResponse? TokenResponse { get; set; }
    public bool RequiresAccountLinking { get; set; }
    public ExternalLoginInfoDto? ExternalLoginInfo { get; set; }
    public string? RedirectUrl { get; set; }
}
