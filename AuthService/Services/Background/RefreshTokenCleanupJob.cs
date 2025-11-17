using AuthService.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthService.Services.Background;

public class RefreshTokenCleanupJob : ISecurityJob
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<RefreshTokenCleanupJob> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _refreshTokenRepository.DeleteExpiredTokensAsync();
            _logger.LogInformation("Expired refresh tokens cleaned up at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up expired refresh tokens");
        }
    }
}

