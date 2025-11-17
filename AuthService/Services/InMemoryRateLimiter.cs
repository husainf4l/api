using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Services;

public interface IRateLimiter
{
    bool TryAcquire(string key, int limit, TimeSpan window, out TimeSpan retryAfter);
}

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;

    public InMemoryRateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryAcquire(string key, int limit, TimeSpan window, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        var entry = _cache.GetOrCreate(key, cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = window;
            return new RateLimitBucket
            {
                Count = 0,
                Window = window,
                CreatedAt = DateTime.UtcNow
            };
        })!;

        if (entry.Count >= limit)
        {
            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            retryAfter = entry.Window - elapsed;
            if (retryAfter < TimeSpan.Zero)
            {
                retryAfter = TimeSpan.Zero;
            }
            return false;
        }

        entry.Count++;
        return true;
    }

    private sealed class RateLimitBucket
    {
        public int Count { get; set; }
        public TimeSpan Window { get; set; } = TimeSpan.Zero;
        public DateTime CreatedAt { get; set; }
    }
}

