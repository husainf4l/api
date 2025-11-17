using AuthService.Data;
using AuthService.Models.Entities;
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace AuthService.GraphQL.DataLoaders;

public class UserRolesDataLoader : GroupedDataLoader<Guid, UserRole>
{
    private readonly IDbContextFactory<AuthDbContext> _dbContextFactory;

    public UserRolesDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<AuthDbContext> dbContextFactory)
        : base(batchScheduler)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<ILookup<Guid, UserRole>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var userRoles = await dbContext.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => keys.Contains(ur.UserId))
            .ToListAsync(cancellationToken);

        return userRoles.ToLookup(ur => ur.UserId);
    }
}
