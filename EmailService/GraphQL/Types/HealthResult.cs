namespace EmailService.GraphQL.Types;

public class HealthResult
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
