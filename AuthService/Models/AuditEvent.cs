namespace AuthService.Models;

public enum AuditEventType
{
    LoginSucceeded,
    LoginFailed,
    RefreshSucceeded,
    RefreshFailed,
    RevokeSucceeded,
    RevokeFailed,
    TokenValidated,
    TokenValidationFailed,
    AccountLocked,
    PasswordChanged
}

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AuditEventType EventType { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

