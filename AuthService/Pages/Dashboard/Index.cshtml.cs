using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly AuthDbContext _context;
    private readonly ApplicationService _applicationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        AuthDbContext context,
        ApplicationService applicationService,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _applicationService = applicationService;
        _logger = logger;
    }

    // Dashboard statistics
    public int TotalApplications { get; set; }
    public int TotalUsers { get; set; }
    public int TotalApiKeys { get; set; }
    public int ActiveApiKeys { get; set; }
    public int TotalSessionsToday { get; set; }
    public int FailedLoginsToday { get; set; }

    // Recent activity
    public List<RecentActivityItem> RecentActivities { get; set; } = new();
    public List<ApplicationSummary> TopApplications { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            // Load statistics
            TotalApplications = await _context.Applications.CountAsync(a => a.IsActive);
            TotalUsers = await _context.Users.CountAsync(u => u.IsActive);
            TotalApiKeys = await _context.ApiKeys.CountAsync();
            ActiveApiKeys = await _context.ApiKeys.CountAsync(ak =>
                ak.IsActive &&
                !ak.IsRevoked &&
                (ak.ExpiresAt == null || ak.ExpiresAt > DateTime.UtcNow));

            // Today's sessions
            var today = DateTime.UtcNow.Date;
            TotalSessionsToday = await _context.SessionLogs
                .CountAsync(s => s.LoginAt.HasValue && s.LoginAt.Value.Date == today && s.IsSuccessful);
            FailedLoginsToday = await _context.SessionLogs
                .CountAsync(s => s.LoginAt.HasValue && s.LoginAt.Value.Date == today && !s.IsSuccessful);

            // Recent activities
            RecentActivities = await GetRecentActivitiesAsync();

            // Top applications
            TopApplications = await GetTopApplicationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            // Set default values if database is not available
            TotalApplications = 0;
            TotalUsers = 0;
            TotalApiKeys = 0;
            ActiveApiKeys = 0;
            TotalSessionsToday = 0;
            FailedLoginsToday = 0;
        }
    }

    private async Task<List<RecentActivityItem>> GetRecentActivitiesAsync()
    {
        var activities = new List<RecentActivityItem>();

        // Recent user registrations
        var recentUsers = await _context.Users
            .Include(u => u.Application)
            .Where(u => u.CreatedAt > DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentActivityItem
            {
                Type = "user_registration",
                Description = $"New user registered: {u.Email}",
                ApplicationName = u.Application.Name,
                Timestamp = u.CreatedAt,
                IconClass = "fas fa-user-plus text-success"
            })
            .ToListAsync();

        activities.AddRange(recentUsers);

        // Recent API key creations
        var recentApiKeys = await _context.ApiKeys
            .Include(ak => ak.Application)
            .Include(ak => ak.OwnerUser)
            .Where(ak => ak.CreatedAt > DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(ak => ak.CreatedAt)
            .Take(3)
            .Select(ak => new RecentActivityItem
            {
                Type = "api_key_created",
                Description = $"API key created: {ak.Name}",
                ApplicationName = ak.Application.Name,
                Timestamp = ak.CreatedAt,
                IconClass = "fas fa-key text-info"
            })
            .ToListAsync();

        activities.AddRange(recentApiKeys);

        // Recent login sessions
        var recentSessions = await _context.SessionLogs
            .Include(s => s.User)
            .Include(s => s.Application)
            .Where(s => s.LoginAt.HasValue && s.LoginAt.Value > DateTime.UtcNow.AddHours(-24) && s.IsSuccessful)
            .OrderByDescending(s => s.LoginAt)
            .Take(5)
            .Select(s => new RecentActivityItem
            {
                Type = "user_login",
                Description = $"User login: {s.User.Email}",
                ApplicationName = s.Application.Name,
                Timestamp = s.LoginAt!.Value,
                IconClass = "fas fa-sign-in-alt text-primary"
            })
            .ToListAsync();

        activities.AddRange(recentSessions);

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();
    }

    private async Task<List<ApplicationSummary>> GetTopApplicationsAsync()
    {
        return await _context.Applications
            .Where(a => a.IsActive)
            .Select(a => new ApplicationSummary
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                UserCount = a.Users.Count(u => u.IsActive),
                ApiKeyCount = a.ApiKeys.Count(),
                ActiveApiKeyCount = a.ApiKeys.Count(ak =>
                    ak.IsActive &&
                    !ak.IsRevoked &&
                    (ak.ExpiresAt == null || ak.ExpiresAt > DateTime.UtcNow)),
                LastActivity = a.Users
                    .Where(u => u.LastLoginAt.HasValue)
                    .Max(u => u.LastLoginAt)
            })
            .OrderByDescending(a => a.UserCount)
            .Take(5)
            .ToListAsync();
    }

    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IconClass { get; set; } = string.Empty;
    }

    public class ApplicationSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int ApiKeyCount { get; set; }
        public int ActiveApiKeyCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
