using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthService.Repositories;

namespace AuthService.Pages;

public class IndexModel : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IApplicationRepository _applicationRepository;

    public IndexModel(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IApplicationRepository applicationRepository)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _applicationRepository = applicationRepository;
    }

    public int TotalUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalApplications { get; set; }
    public int LoginsToday { get; set; }
    public List<ActivityItem> RecentActivities { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get real data from repositories
        // TODO: Add GetAllCount methods to repositories
        TotalUsers = 2; // Placeholder
        TotalApplications = 2; // We have 2 apps registered

        var apps = await _applicationRepository.GetAllAsync();
        TotalApplications = apps.Count;

        // TODO: Implement GetAllActiveSessions
        ActiveSessions = 0;
        LoginsToday = 0;

        // Mock recent activities for now
        RecentActivities = new List<ActivityItem>
        {
            new() { Timestamp = DateTime.UtcNow.AddMinutes(-5), UserEmail = "user@example.com", Action = "Login", Application = "Email Service", IpAddress = "192.168.1.1" },
            new() { Timestamp = DateTime.UtcNow.AddMinutes(-10), UserEmail = "admin@example.com", Action = "Login", Application = "Dashboard", IpAddress = "192.168.1.2" }
        };
    }

    public class ActivityItem
    {
        public DateTime Timestamp { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Application { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }
}
