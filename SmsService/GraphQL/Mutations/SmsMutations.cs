using Microsoft.AspNetCore.Http;
using SmsService.Services;

namespace SmsService.GraphQL.Mutations;

public class SmsMutations
{
    /// <summary>
    /// Send OTP SMS message
    /// </summary>
    public async Task<SmsResult> SendOtp(
        SendSmsInput input,
        [Service] ISmsService smsService,
        HttpContext httpContext)
    {
        var result = await smsService.SendOtpAsync(
            input.To, 
            input.Message, 
            input.SenderId, 
            httpContext);

        return new SmsResult
        {
            Success = result.Success,
            Message = result.Message,
            RawResponse = result.RawResponse
        };
    }

    /// <summary>
    /// Send general SMS message
    /// </summary>
    public async Task<SmsResult> SendGeneral(
        SendSmsInput input,
        [Service] ISmsService smsService,
        HttpContext httpContext)
    {
        var result = await smsService.SendGeneralAsync(
            input.To, 
            input.Message, 
            input.SenderId, 
            httpContext);

        return new SmsResult
        {
            Success = result.Success,
            Message = result.Message,
            RawResponse = result.RawResponse
        };
    }

    /// <summary>
    /// Send bulk SMS to multiple recipients (up to 120)
    /// </summary>
    public async Task<SmsResult> SendBulk(
        SendBulkSmsInput input,
        [Service] ISmsService smsService,
        HttpContext httpContext)
    {
        var result = await smsService.SendBulkAsync(
            input.To, 
            input.Message, 
            input.SenderId, 
            httpContext);

        return new SmsResult
        {
            Success = result.Success,
            Message = result.Message,
            RawResponse = result.RawResponse
        };
    }
}

public class SendSmsInput
{
    public required string To { get; set; }
    public required string Message { get; set; }
    public string? SenderId { get; set; }
}

public class SendBulkSmsInput
{
    public required List<string> To { get; set; }
    public required string Message { get; set; }
    public string? SenderId { get; set; }
}

public class SmsResult
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public string? RawResponse { get; set; }
}
