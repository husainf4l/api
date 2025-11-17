using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public interface IPasswordHistoryRepository
{
    Task AddAsync(PasswordHistory history, CancellationToken cancellationToken = default);
    Task<bool> HasRecentReuseAsync(Guid userId, string passwordHash, int lookbackCount = 5, CancellationToken cancellationToken = default);
}

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly AuthDbContext _context;

    public PasswordHistoryRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordHistory history, CancellationToken cancellationToken = default)
    {
        _context.PasswordHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasRecentReuseAsync(Guid userId, string passwordHash, int lookbackCount = 5, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(lookbackCount)
            .AnyAsync(ph => ph.PasswordHash == passwordHash, cancellationToken);
    }
}

