namespace SmsService.Models;

public class BalanceResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public double? Balance { get; set; }
    public string? RawResponse { get; set; }
}

public class SmsResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public string? RawResponse { get; set; }
}
