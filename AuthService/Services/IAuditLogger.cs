using System.Collections.Generic;
using AuthService.Models;

namespace AuthService.Services;

public interface IAuditLogger
{
    Task LogAsync(AuditEventType type, AuditEventContext context, CancellationToken cancellationToken = default);
}

public class AuditEventContext
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceInfo { get; init; }
    public Dictionary<string, object?>? Extras { get; init; }
}

