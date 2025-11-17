using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthService.Services.Background;

public class SecurityJobScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SecurityJobScheduler> _logger;
    private readonly TimeSpan _cleanupInterval;

    public SecurityJobScheduler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SecurityJobScheduler> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        var minutes = configuration.GetValue("Security:Cleanup:IntervalMinutes", 60);
        _cleanupInterval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security job scheduler started with interval {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var jobs = scope.ServiceProvider.GetServices<ISecurityJob>();

            foreach (var job in jobs)
            {
                try
                {
                    _logger.LogInformation("Running security job {Job}", job.GetType().Name);
                    await job.ExecuteAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Security job {Job} failed", job.GetType().Name);
                }
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}

