using Microsoft.AspNetCore.Mvc;
using SmsService.DTOs;
using SmsService.Services;

namespace SmsService.Controllers;

[ApiController]
[Route("api/sms")]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(ISmsService smsService, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "SMS service is healthy" });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var result = await _smsService.GetBalanceAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return StatusCode(500, new { error = "Failed to get balance", message = ex.Message });
        }
    }

    [HttpPost("send/otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendSmsRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "To and Message are required" });
            }

            var result = await _smsService.SendOtpAsync(request.To, request.Message, request.SenderId);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP SMS");
            return StatusCode(500, new { error = "Failed to send OTP SMS", message = ex.Message });
        }
    }

    [HttpPost("send/general")]
    public async Task<IActionResult> SendGeneral([FromBody] SendSmsRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "To and Message are required" });
            }

            var result = await _smsService.SendGeneralAsync(request.To, request.Message, request.SenderId);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending General SMS");
            return StatusCode(500, new { error = "Failed to send SMS", message = ex.Message });
        }
    }

    [HttpPost("send/bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkSmsRequest request)
    {
        try
        {
            if (request.To == null || request.To.Count == 0 || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "To numbers and Message are required" });
            }

            if (request.To.Count > 120)
            {
                return BadRequest(new { error = "Maximum 120 numbers allowed for bulk SMS" });
            }

            var result = await _smsService.SendBulkAsync(request.To, request.Message, request.SenderId);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Bulk SMS");
            return StatusCode(500, new { error = "Failed to send bulk SMS", message = ex.Message });
        }
    }
}
