using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> CreateAsync(UserSession session);
    Task<UserSession?> GetActiveSessionAsync(Guid userId, Guid applicationId);
    Task<List<UserSession>> GetUserSessionsAsync(Guid userId);
    Task<List<UserSession>> GetApplicationSessionsAsync(Guid applicationId);
    Task EndSessionAsync(Guid sessionId);
    Task UpdateLastActivityAsync(Guid sessionId);
}

public class UserSessionRepository : IUserSessionRepository
{
    private readonly AuthDbContext _context;

    public UserSessionRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession> CreateAsync(UserSession session)
    {
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<UserSession?> GetActiveSessionAsync(Guid userId, Guid applicationId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.ApplicationId == applicationId && s.IsActive)
            .OrderByDescending(s => s.LoginAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserSession>> GetUserSessionsAsync(Guid userId)
    {
        return await _context.UserSessions
            .Include(s => s.Application)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoginAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<UserSession>> GetApplicationSessionsAsync(Guid applicationId)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.ApplicationId == applicationId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt ?? s.LoginAt)
            .ToListAsync();
    }

    public async Task EndSessionAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastActivityAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
