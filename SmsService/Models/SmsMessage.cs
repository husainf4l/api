namespace SmsService.Models;

public class SmsMessage
{
    public int Id { get; set; }
    public required string Recipient { get; set; }
    public required string Message { get; set; }
    public required string SenderId { get; set; }
    public string? MessageId { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ResponseData { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ApiKeyUsed { get; set; }
    public string? AppName { get; set; }
    public string? AppVersion { get; set; }
}
