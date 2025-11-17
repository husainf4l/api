using System.Security.Cryptography;
using System.Text.Json;
using AuthService.Data;
using AuthService.Models.DTOs;
using OtpNet;
using QRCoder;

namespace AuthService.Services;

public interface ITwoFactorService
{
    Task<TwoFactorSetupResult> SetupTwoFactorAsync(Guid userId, string issuer = "AuthService");
    Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(Guid userId);
    Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code);
    Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode);
    Task<bool> RegenerateBackupCodesAsync(Guid userId);
    Task<TwoFactorStatus> GetTwoFactorStatusAsync(Guid userId);
}

public class TwoFactorService : ITwoFactorService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(AuthDbContext context, ILogger<TwoFactorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TwoFactorSetupResult> SetupTwoFactorAsync(Guid userId, string issuer = "AuthService")
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new TwoFactorSetupResult { Success = false, Error = "User not found" };
            }

            if (user.TwoFactorEnabled)
            {
                return new TwoFactorSetupResult { Success = false, Error = "Two-factor authentication is already enabled" };
            }

            // Generate a new secret key
            var secretBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(secretBytes);

            var secret = Base32Encoding.ToString(secretBytes);
            var secretBytesDecoded = Base32Encoding.ToBytes(secret);

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();

            // Update user
            user.TwoFactorSecret = secret;
            user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);

            await _context.SaveChangesAsync();

            // Generate TOTP URI for QR code
            var totp = new Totp(secretBytesDecoded);
            var accountName = user.Email;
            var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            // Generate QR code
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            _logger.LogInformation("Two-factor authentication setup initiated for user {UserId}", userId);

            return new TwoFactorSetupResult
            {
                Success = true,
                Secret = secret,
                QrCodeUri = uri,
                QrCodeImage = qrCodeImage,
                BackupCodes = backupCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up two-factor authentication for user {UserId}", userId);
            return new TwoFactorSetupResult { Success = false, Error = "Failed to setup two-factor authentication" };
        }
    }

    public async Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return false;
            }

            // Verify the code
            var isValid = await VerifyTwoFactorCodeAsync(userId, verificationCode);
            if (!isValid)
            {
                return false;
            }

            // Enable 2FA
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Two-factor authentication enabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorBackupCodes = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return false;
            }

            var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new Totp(secretBytes);

            // Verify the code (allowing for clock skew)
            var isValid = totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);

            if (isValid)
            {
                _logger.LogInformation("Two-factor code verified successfully for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorBackupCodes))
            {
                return false;
            }

            var backupCodes = JsonSerializer.Deserialize<List<string>>(user.TwoFactorBackupCodes);
            if (backupCodes == null || !backupCodes.Contains(backupCode))
            {
                return false;
            }

            // Remove the used backup code
            backupCodes.Remove(backupCode);
            user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Backup code verified and removed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying backup code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RegenerateBackupCodesAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            var backupCodes = GenerateBackupCodes();
            user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Backup codes regenerated for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating backup codes for user {UserId}", userId);
            return false;
        }
    }

    public async Task<TwoFactorStatus> GetTwoFactorStatusAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new TwoFactorStatus { IsEnabled = false };
            }

            return new TwoFactorStatus
            {
                IsEnabled = user.TwoFactorEnabled,
                IsConfigured = !string.IsNullOrEmpty(user.TwoFactorSecret),
                HasBackupCodes = !string.IsNullOrEmpty(user.TwoFactorBackupCodes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting two-factor status for user {UserId}", userId);
            return new TwoFactorStatus { IsEnabled = false };
        }
    }

    private List<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = BitConverter.ToUInt32(bytes, 0) % 1000000; // 6-digit code
            codes.Add(code.ToString("D6"));
        }

        return codes;
    }
}

public class TwoFactorSetupResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Secret { get; set; }
    public string? QrCodeUri { get; set; }
    public byte[]? QrCodeImage { get; set; }
    public List<string>? BackupCodes { get; set; }
}

public class TwoFactorStatus
{
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
    public bool HasBackupCodes { get; set; }
}
