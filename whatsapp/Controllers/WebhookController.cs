using Microsoft.AspNetCore.Mvc;
using WhatsApp.Models;
using WhatsApp.Services;

namespace WhatsApp.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly WhatsAppService _whatsAppService;

    public WebhookController(
        ILogger<WebhookController> logger,
        IConfiguration configuration,
        WhatsAppService whatsAppService)
    {
        _logger = logger;
        _configuration = configuration;
        _whatsAppService = whatsAppService;
    }

    /// <summary>
    /// Webhook verification endpoint (GET)
    /// Meta/Facebook uses this to verify your webhook
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var verifyToken = _configuration["WHATSAPP_VERIFY_TOKEN"];

        _logger.LogInformation("Webhook verification request - Mode: {Mode}, Token: {Token}, Expected: {Expected}", 
            mode, token, verifyToken);

        if (mode == "subscribe" && token == verifyToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(challenge);
        }

        _logger.LogWarning("Webhook verification failed - Mode match: {ModeMatch}, Token match: {TokenMatch}", 
            mode == "subscribe", token == verifyToken);
        return Unauthorized("Invalid verify token");
    }

    /// <summary>
    /// Webhook event endpoint (POST)
    /// Receives incoming messages and status updates from WhatsApp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook([FromBody] WebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("Received webhook event");

            await _whatsAppService.HandleWebhookAsync(payload);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook");
            // Return 200 to avoid Meta retrying
            return Ok(new { success = false, error = ex.Message });
        }
    }
}
