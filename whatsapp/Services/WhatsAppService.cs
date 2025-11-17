using System.Text.Json;
using WhatsApp.Models;

namespace WhatsApp.Services;

public class WhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenManagerService _tokenManager;

    public WhatsAppService(
        ILogger<WhatsAppService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        TokenManagerService tokenManager)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _tokenManager = tokenManager;
    }

    public async Task<TemplateResponse> ListTemplatesAsync()
    {
        var businessAccountId = _configuration["WHATSAPP_BUSINESS_ACCOUNT_ID"];
        var apiVersion = _configuration["WHATSAPP_API_VERSION"] ?? "v18.0";
        var accessToken = await _tokenManager.GetAccessTokenAsync();

        _logger.LogDebug("Using access token: {Token}", accessToken);

        var url = $"https://graph.facebook.com/{apiVersion}/{businessAccountId}/message_templates";
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to list templates. Error: {Error}", errorContent);
            throw new HttpRequestException($"Failed to list templates: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TemplateResponse>(json, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        return result ?? new TemplateResponse();
    }

    public async Task<SendMessageResponse> SendTemplateMessageAsync(
        string to, 
        string templateName, 
        string? language = null, 
        List<string>? parameters = null)
    {
        // Set default language based on template name
        if (string.IsNullOrEmpty(language))
        {
            var templateLanguageMap = new Dictionary<string, string>
            {
                { "hello_world", "en_US" },
                { "cv_received_notification", "en" },
                { "temppassword", "en_US" }
            };

            language = templateLanguageMap.GetValueOrDefault(templateName, "en_US");
        }

        var phoneNumberId = _configuration["WHATSAPP_PHONE_NUMBER_ID"];
        var apiVersion = _configuration["WHATSAPP_API_VERSION"] ?? "v18.0";
        var accessToken = await _tokenManager.GetAccessTokenAsync();
        var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

        // Build components for BODY and BUTTON if params are provided
        object? components = null;
        if (parameters != null && parameters.Count > 0)
        {
            if (templateName == "temppassword")
            {
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = parameters[0] }
                        }
                    },
                    new
                    {
                        type = "button",
                        sub_type = "url",
                        index = 0,
                        parameters = new[]
                        {
                            new { type = "text", text = parameters[0] }
                        }
                    }
                };
            }
            else
            {
                var componentsList = new List<object>
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = parameters[0] }
                        }
                    }
                };

                // If a second param is provided, add a URL button parameter
                if (parameters.Count > 1)
                {
                    componentsList.Add(new
                    {
                        type = "button",
                        sub_type = "url",
                        index = 0,
                        parameters = new[]
                        {
                            new { type = "text", text = parameters[1] }
                        }
                    });
                }

                components = componentsList;
            }
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = language },
                components
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        _logger.LogInformation("Sending WhatsApp template message: {Payload}", json);

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var resultJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error sending template message: {Error}", resultJson);
            var errorResponse = JsonSerializer.Deserialize<WhatsAppError>(resultJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            throw new HttpRequestException($"WhatsApp API error: {errorResponse?.Error?.Message ?? "Unknown error"}");
        }

        _logger.LogInformation("Template message sent: {Result}", resultJson);
        
        var result = JsonSerializer.Deserialize<SendMessageResponse>(resultJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        return result ?? new SendMessageResponse();
    }

    public async Task<SendMessageResponse> SendTextMessageAsync(string to, string text)
    {
        var phoneNumberId = _configuration["WHATSAPP_PHONE_NUMBER_ID"];
        var apiVersion = _configuration["WHATSAPP_API_VERSION"] ?? "v18.0";
        var accessToken = await _tokenManager.GetAccessTokenAsync();
        var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = text }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var resultJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error sending text message: {Error}", resultJson);
            var errorResponse = JsonSerializer.Deserialize<WhatsAppError>(resultJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            throw new HttpRequestException($"WhatsApp API error: {errorResponse?.Error?.Message ?? "Unknown error"}");
        }

        _logger.LogInformation("Text message sent: {Result}", resultJson);

        var result = JsonSerializer.Deserialize<SendMessageResponse>(resultJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        return result ?? new SendMessageResponse();
    }

    /// <summary>
    /// Handle incoming WhatsApp webhook payloads
    /// Processes messages, status updates, and other webhook events
    /// </summary>
    public async Task HandleWebhookAsync(WebhookPayload payload)
    {
        _logger.LogInformation("Processing WhatsApp webhook payload");

        try
        {
            // Check if this is a valid webhook payload
            if (payload.Object != "whatsapp_business_account")
            {
                _logger.LogWarning("Invalid webhook payload object: {Object}", payload.Object);
                return;
            }

            if (payload.Entry == null || !payload.Entry.Any())
            {
                _logger.LogWarning("Invalid webhook payload entry");
                return;
            }

            // Process each entry
            foreach (var entry in payload.Entry)
            {
                if (entry.Messaging != null && entry.Messaging.Any())
                {
                    await ProcessMessagesAsync(entry.Messaging);
                }

                if (entry.Changes != null && entry.Changes.Any())
                {
                    await ProcessChangesAsync(entry.Changes);
                }
            }

            _logger.LogInformation("Webhook processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            throw;
        }
    }

    /// <summary>
    /// Process messaging events from webhook
    /// </summary>
    private async Task ProcessMessagesAsync(List<WebhookMessaging> messaging)
    {
        foreach (var message in messaging)
        {
            try
            {
                // Handle message status updates
                if (message.Message?.Id != null)
                {
                    await HandleMessageStatusAsync(message);
                }

                // Handle incoming messages
                if (message.Message?.Type != null && message.Contacts != null)
                {
                    await HandleIncomingMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Process changes from webhook
    /// </summary>
    private async Task ProcessChangesAsync(List<WebhookChange> changes)
    {
        foreach (var change in changes)
        {
            if (change.Field == "messages")
            {
                _logger.LogInformation("Processing message changes: {Change}", JsonSerializer.Serialize(change));
            }
            else if (change.Field == "message_template_status_update")
            {
                _logger.LogInformation("Processing template status update: {Change}", JsonSerializer.Serialize(change));
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle message status updates (delivered, read, etc.)
    /// </summary>
    private async Task HandleMessageStatusAsync(WebhookMessaging message)
    {
        var messageId = message.Message?.Id;
        var timestamp = message.Timestamp;

        _logger.LogInformation("Message status update - ID: {MessageId}, Timestamp: {Timestamp}", 
            messageId, timestamp);

        // Here you could update message status in database
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle incoming messages from users
    /// </summary>
    private async Task HandleIncomingMessageAsync(WebhookMessaging message)
    {
        var from = message.Contacts?.FirstOrDefault()?.WaId;
        var messageData = message.Message;
        var timestamp = message.Timestamp;

        _logger.LogInformation("Incoming message from {From}: {Message}", from, JsonSerializer.Serialize(messageData));

        // Handle different message types
        switch (messageData?.Type)
        {
            case "text":
                await HandleTextMessageAsync(from!, messageData.Text?.Body!, timestamp!);
                break;
            case "image":
                _logger.LogInformation("Received image from {From}", from);
                break;
            case "document":
                _logger.LogInformation("Received document from {From}", from);
                break;
            default:
                _logger.LogInformation("Received {Type} message from {From}", messageData?.Type, from);
                break;
        }
    }

    /// <summary>
    /// Handle incoming text messages
    /// </summary>
    private async Task HandleTextMessageAsync(string from, string text, string timestamp)
    {
        _logger.LogInformation("Processing text message from {From}: {Text}", from, text);

        // Here you could implement auto-responses, forward to customer service, etc.
        // Example: Send an auto-reply
        // await SendTextMessageAsync(from, "Thank you for your message. We will get back to you soon.");

        await Task.CompletedTask;
    }
}
