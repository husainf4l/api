namespace EmailService.GraphQL.Types;

public class GenerateHtmlResult
{
    public bool Success { get; set; }
    public string Html { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
