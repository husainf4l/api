using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public interface IApplicationRepository
{
    Task<Application?> GetByClientIdAsync(string clientId);
    Task<Application?> GetByIdAsync(Guid id);
    Task<List<Application>> GetAllAsync();
    Task<Application> CreateAsync(Application application);
    Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret);
}

public class ApplicationRepository : IApplicationRepository
{
    private readonly AuthDbContext _context;
    private readonly Services.IPasswordService _passwordService;

    public ApplicationRepository(AuthDbContext context, Services.IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<Application?> GetByClientIdAsync(string clientId)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.ClientId == clientId && a.IsActive);
    }

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        return await _context.Applications.FindAsync(id);
    }

    public async Task<List<Application>> GetAllAsync()
    {
        return await _context.Applications
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Application> CreateAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        var application = await GetByClientIdAsync(clientId);
        if (application == null)
        {
            return false;
        }

        return _passwordService.VerifyPassword(clientSecret, application.ClientSecret);
    }
}
