using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthService.Repositories;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Pages.Sessions;

public class IndexModel : PageModel
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly Data.AuthDbContext _context;

    public IndexModel(
        IUserSessionRepository sessionRepository,
        IApplicationRepository applicationRepository,
        Data.AuthDbContext context)
    {
        _sessionRepository = sessionRepository;
        _applicationRepository = applicationRepository;
        _context = context;
    }

    public List<UserSession> Sessions { get; set; } = new();
    public List<Application> Applications { get; set; } = new();

    public async Task OnGetAsync()
    {
        Applications = await _applicationRepository.GetAllAsync();
        
        // Get all active sessions with related data
        Sessions = await _context.UserSessions
            .Include(s => s.User)
            .Include(s => s.Application)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LoginAt)
            .ToListAsync();
    }
}
