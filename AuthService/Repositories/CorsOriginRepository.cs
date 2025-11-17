using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public interface ICorsOriginRepository
{
    Task<List<string>> GetActiveOriginsAsync(CancellationToken cancellationToken = default);
}

public class CorsOriginRepository : ICorsOriginRepository
{
    private readonly AuthDbContext _context;

    public CorsOriginRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetActiveOriginsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CorsOrigins
            .Where(origin => origin.IsActive)
            .OrderBy(origin => origin.Origin)
            .Select(origin => origin.Origin)
            .ToListAsync(cancellationToken);
    }
}

