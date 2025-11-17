using System.Threading;

namespace AuthService.Services.Background;

public interface ISecurityJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

