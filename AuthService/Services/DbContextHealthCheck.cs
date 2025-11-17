using AuthService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuthService.Services;

public class DbContextHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AuthDbContext> _contextFactory;

    public DbContextHealthCheck(IDbContextFactory<AuthDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
