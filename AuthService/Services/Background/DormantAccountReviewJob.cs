using AuthService.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthService.Services.Background;

public class DormantAccountReviewJob : ISecurityJob
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DormantAccountReviewJob> _logger;
    private readonly TimeSpan _dormantThreshold;

    public DormantAccountReviewJob(
        IUserRepository userRepository,
        ILogger<DormantAccountReviewJob> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _logger = logger;
        var days = configuration.GetValue("Security:DormantAccountDays", 180);
        _dormantThreshold = TimeSpan.FromDays(days);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - _dormantThreshold;
            var dormantUsers = await _userRepository.GetDormantUsersAsync(cutoff, cancellationToken);

            _logger.LogInformation("Found {Count} dormant accounts older than {Days} days", dormantUsers.Count, _dormantThreshold.TotalDays);

            foreach (var user in dormantUsers)
            {
                // Future: enqueue notification or deactivate
                _logger.LogWarning("Dormant user detected: {UserId} {Email}", user.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review dormant accounts");
        }
    }
}

