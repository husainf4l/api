using SmsService.Models;
using SmsService.Repositories;
using SmsService.Services;

namespace SmsService.GraphQL.Queries;

public class SmsQueries
{
    /// <summary>
    /// Health check for the SMS service
    /// </summary>
    public string Health() => "SMS service is healthy";

    /// <summary>
    /// Get list of available sender IDs
    /// </summary>
    public List<string> Senders([Service] ISmsService smsService)
    {
        return smsService.GetAvailableSenders();
    }

    /// <summary>
    /// Get SMS balance from JOSMS
    /// </summary>
    public async Task<BalanceResult> Balance([Service] ISmsService smsService)
    {
        var result = await smsService.GetBalanceAsync();
        return new BalanceResult
        {
            Success = result.Success,
            Message = result.Message,
            Balance = result.Balance
        };
    }

    /// <summary>
    /// Get message history with optional limit
    /// </summary>
    public async Task<MessageHistoryResult> History(
        [Service] SmsMessageRepository repository,
        int limit = 100)
    {
        var messages = await repository.GetRecentMessagesAsync(limit);
        return new MessageHistoryResult
        {
            Success = true,
            Count = messages.Count,
            Messages = messages
        };
    }

    /// <summary>
    /// Get message history for a specific recipient
    /// </summary>
    public async Task<MessageHistoryResult> HistoryByRecipient(
        [Service] SmsMessageRepository repository,
        string recipient)
    {
        var messages = await repository.GetMessagesByRecipientAsync(recipient);
        return new MessageHistoryResult
        {
            Success = true,
            Count = messages.Count,
            Messages = messages,
            Recipient = recipient
        };
    }
}

public class BalanceResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public double? Balance { get; set; }
}

public class MessageHistoryResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<SmsMessage> Messages { get; set; } = new();
    public string? Recipient { get; set; }
}
