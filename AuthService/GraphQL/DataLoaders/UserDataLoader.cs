using AuthService.Data;
using AuthService.Models.Entities;
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace AuthService.GraphQL.DataLoaders;

public class UserDataLoader : BatchDataLoader<Guid, User>
{
    private readonly IDbContextFactory<AuthDbContext> _dbContextFactory;

    public UserDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<AuthDbContext> dbContextFactory)
        : base(batchScheduler)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var users = await dbContext.Users
            .Where(u => keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return users;
    }
}
