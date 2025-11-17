using System.Collections.Immutable;
using AuthService.Repositories;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace AuthService.Services;

public static class CorsPolicyNames
{
    public const string DynamicCors = "DynamicCors";
}

public class DatabaseCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseCorsPolicyProvider> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private ImmutableArray<string> _cachedOrigins = ImmutableArray<string>.Empty;
    private DateTime _lastFetchedAt = DateTime.MinValue;
    private readonly object _sync = new();

    public DatabaseCorsPolicyProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseCorsPolicyProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext? context, string? policyName)
    {
        if (!string.Equals(policyName, CorsPolicyNames.DynamicCors, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<CorsPolicy?>(null);
        }

        var cacheExpired = DateTime.UtcNow - _lastFetchedAt > _cacheDuration;

        if (cacheExpired)
        {
            lock (_sync)
            {
                if (DateTime.UtcNow - _lastFetchedAt > _cacheDuration)
                {
                    RefreshCache();
                }
            }
        }

        return Task.FromResult(BuildPolicy(_cachedOrigins));
    }

    private void RefreshCache()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICorsOriginRepository>();
            var origins = repository.GetActiveOriginsAsync().GetAwaiter().GetResult();
            _cachedOrigins = origins.ToImmutableArray();
            _lastFetchedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh CORS origins from database");
            // Preserve existing cache; if empty, deny all origins.
        }
    }

    private static CorsPolicy BuildPolicy(ImmutableArray<string> origins)
    {
        var policyBuilder = new CorsPolicyBuilder();

        if (origins.Length == 0)
        {
            // No origins configured -> explicitly disallow all cross-origin requests.
            policyBuilder.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policyBuilder.WithOrigins(origins.ToArray());
        }

        policyBuilder.AllowAnyHeader()
                     .AllowAnyMethod();

        return policyBuilder.Build();
    }
}

