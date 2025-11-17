using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthService.Pages.Audit;

public class IndexModel : PageModel
{
    public List<AuditLogItem> AuditLogs { get; set; } = new();
    public int TotalPages { get; set; } = 1;

    public void OnGet()
    {
        // Mock audit logs for now
        AuditLogs = new List<AuditLogItem>
        {
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow.AddMinutes(-5), EventType = "Login", UserEmail = "john@example.com", Application = "Email Service", IpAddress = "192.168.1.1", UserAgent = "Mozilla/5.0 Chrome/120", Success = true },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow.AddMinutes(-10), EventType = "Failed Login", UserEmail = "test@example.com", Application = "Dashboard", IpAddress = "192.168.1.2", UserAgent = "Mozilla/5.0 Firefox/120", Success = false },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow.AddMinutes(-15), EventType = "Register", UserEmail = "newuser@example.com", Application = "Email Service", IpAddress = "192.168.1.3", UserAgent = "Mozilla/5.0 Safari/17", Success = true },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow.AddMinutes(-20), EventType = "Token Refresh", UserEmail = "john@example.com", Application = "Email Service", IpAddress = "192.168.1.1", UserAgent = "Mozilla/5.0 Chrome/120", Success = true },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow.AddMinutes(-25), EventType = "Logout", UserEmail = "admin@example.com", Application = "Dashboard", IpAddress = "192.168.1.4", UserAgent = "Mozilla/5.0 Edge/120", Success = true }
        };
    }

    public class AuditLogItem
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Application { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
