using AuthService.Data;
using AuthService.Models.Entities;
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace AuthService.GraphQL.DataLoaders;

public class ApplicationDataLoader : BatchDataLoader<Guid, Application>
{
    private readonly IDbContextFactory<AuthDbContext> _dbContextFactory;

    public ApplicationDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options,
        IDbContextFactory<AuthDbContext> dbContextFactory)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, Application>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var applications = await dbContext.Applications
            .Where(a => keys.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return applications;
    }
}
