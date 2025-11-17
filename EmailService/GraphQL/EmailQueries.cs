using EmailService.GraphQL.Types;

namespace EmailService.GraphQL;

public class EmailQueries
{
    public HealthResult Health()
    {
        return new HealthResult
        {
            Status = "healthy",
            Service = "EmailService",
            Timestamp = DateTime.UtcNow
        };
    }
}
