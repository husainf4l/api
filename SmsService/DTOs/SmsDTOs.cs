namespace SmsService.DTOs;

public class SendSmsRequest
{
    public required string To { get; set; }
    public required string Message { get; set; }
    public string? SenderId { get; set; }
}

public class SendBulkSmsRequest
{
    public required List<string> To { get; set; }
    public required string Message { get; set; }
    public string? SenderId { get; set; }
}

public class SmsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
}

public class BalanceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? Balance { get; set; }
    public string? RawResponse { get; set; }
}
