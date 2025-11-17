namespace EmailService.GraphQL.Types;

public class EmailResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? MessageId { get; set; }
}
