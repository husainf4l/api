using System.Net;
using System.Web;
using SmsService.DTOs;

namespace SmsService.Services;

public interface ISmsService
{
    Task<BalanceResponse> GetBalanceAsync();
    Task<SmsResponse> SendOtpAsync(string to, string message, string? senderId = null);
    Task<SmsResponse> SendGeneralAsync(string to, string message, string? senderId = null);
    Task<SmsResponse> SendBulkAsync(List<string> numbers, string message, string? senderId = null);
}

public class JosmsSmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JosmsSmsService> _logger;
    private readonly string _baseUrl;
    private readonly string _accName;
    private readonly string _accPassword;
    private readonly string _defaultSenderId;

    public JosmsSmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<JosmsSmsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _baseUrl = _configuration["JosmsSettings:BaseUrl"] ?? "https://www.josms.net";
        _accName = _configuration["JosmsSettings:AccName"] ?? throw new Exception("JOSMS AccName not configured");
        _accPassword = _configuration["JosmsSettings:AccPassword"] ?? throw new Exception("JOSMS AccPassword not configured");
        _defaultSenderId = _configuration["JosmsSettings:DefaultSenderId"] ?? "MargoGroup";
    }

    public async Task<BalanceResponse> GetBalanceAsync()
    {
        try
        {
            var url = $"{_baseUrl}/SMS/API/GetBalance?AccName={_accName}&AccPass={HttpUtility.UrlEncode(_accPassword)}";
            
            _logger.LogInformation("Getting balance from JOSMS");
            
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Try to parse balance from response
                if (decimal.TryParse(content.Trim(), out decimal balance))
                {
                    return new BalanceResponse
                    {
                        Success = true,
                        Message = "Balance retrieved successfully",
                        Balance = balance,
                        RawResponse = content
                    };
                }

                return new BalanceResponse
                {
                    Success = true,
                    Message = "Balance retrieved",
                    RawResponse = content
                };
            }

            _logger.LogError("Failed to get balance: {Content}", content);
            return new BalanceResponse
            {
                Success = false,
                Message = $"Failed to retrieve balance: {response.StatusCode}",
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return new BalanceResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<SmsResponse> SendOtpAsync(string to, string message, string? senderId = null)
    {
        try
        {
            var sender = senderId ?? _defaultSenderId;
            var normalizedNumber = NormalizePhoneNumber(to);

            if (normalizedNumber == null)
            {
                return new SmsResponse
                {
                    Success = false,
                    Message = "Invalid phone number format. Must be 962XXXXXXXXX"
                };
            }

            var url = $"{_baseUrl}/SMSServices/Clients/Prof/RestSingleSMS/SendSMS" +
                     $"?senderid={sender}" +
                     $"&numbers={normalizedNumber}" +
                     $"&accname={_accName}" +
                     $"&AccPass={HttpUtility.UrlEncode(_accPassword)}" +
                     $"&msg={HttpUtility.UrlEncode(message)}";

            _logger.LogInformation("Sending OTP SMS to {Number}", normalizedNumber);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return new SmsResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "OTP SMS sent successfully" : "Failed to send OTP SMS",
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP SMS to {Number}", to);
            return new SmsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<SmsResponse> SendGeneralAsync(string to, string message, string? senderId = null)
    {
        try
        {
            var sender = senderId ?? _defaultSenderId;
            var normalizedNumber = NormalizePhoneNumber(to);

            if (normalizedNumber == null)
            {
                return new SmsResponse
                {
                    Success = false,
                    Message = "Invalid phone number format. Must be 962XXXXXXXXX"
                };
            }

            var url = $"{_baseUrl}/SMSServices/Clients/Prof/RestSingleSMS_General/SendSMS" +
                     $"?senderid={sender}" +
                     $"&numbers={normalizedNumber}" +
                     $"&accname={_accName}" +
                     $"&AccPass={HttpUtility.UrlEncode(_accPassword)}" +
                     $"&msg={HttpUtility.UrlEncode(message)}";

            _logger.LogInformation("Sending General SMS to {Number}", normalizedNumber);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return new SmsResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "SMS sent successfully" : "Failed to send SMS",
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending General SMS to {Number}", to);
            return new SmsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<SmsResponse> SendBulkAsync(List<string> numbers, string message, string? senderId = null)
    {
        try
        {
            if (numbers.Count > 120)
            {
                return new SmsResponse
                {
                    Success = false,
                    Message = "Bulk SMS limited to 120 numbers maximum"
                };
            }

            var sender = senderId ?? _defaultSenderId;
            var normalizedNumbers = numbers
                .Select(NormalizePhoneNumber)
                .Where(n => n != null)
                .ToList();

            if (normalizedNumbers.Count == 0)
            {
                return new SmsResponse
                {
                    Success = false,
                    Message = "No valid phone numbers provided"
                };
            }

            var numbersParam = string.Join(",", normalizedNumbers);
            var url = $"{_baseUrl}/sms/api/SendBulkMessages.cfm" +
                     $"?numbers={numbersParam}" +
                     $"&senderid={sender}" +
                     $"&AccName={_accName}" +
                     $"&AccPass={HttpUtility.UrlEncode(_accPassword)}" +
                     $"&msg={HttpUtility.UrlEncode(message)}" +
                     $"&requesttimeout=5000000";

            _logger.LogInformation("Sending Bulk SMS to {Count} numbers", normalizedNumbers.Count);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return new SmsResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode 
                    ? $"Bulk SMS sent successfully to {normalizedNumbers.Count} numbers" 
                    : "Failed to send bulk SMS",
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Bulk SMS");
            return new SmsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private string? NormalizePhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle different formats
        if (digits.StartsWith("00962"))
        {
            digits = digits.Substring(2); // Remove 00
        }
        else if (digits.StartsWith("+962"))
        {
            digits = digits.Substring(1); // Remove +
        }
        else if (digits.StartsWith("0") && digits.Length == 10)
        {
            digits = "962" + digits.Substring(1); // Replace leading 0 with 962
        }
        else if (!digits.StartsWith("962"))
        {
            return null; // Invalid format
        }

        // Validate format: 962 + operator code (77, 78, 79) + 7 digits
        if (digits.Length != 12)
        {
            return null;
        }

        var operatorCode = digits.Substring(3, 2);
        if (operatorCode != "77" && operatorCode != "78" && operatorCode != "79")
        {
            return null;
        }

        return digits;
    }
}
