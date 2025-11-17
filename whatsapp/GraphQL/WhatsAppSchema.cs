using WhatsApp.Models;
using WhatsApp.Services;

namespace WhatsApp.GraphQL;

public class Query
{
    /// <summary>
    /// List all WhatsApp message templates
    /// </summary>
    public async Task<TemplateResponse> ListTemplates([Service] WhatsAppService whatsAppService)
    {
        return await whatsAppService.ListTemplatesAsync();
    }

    /// <summary>
    /// Get token information for debugging
    /// </summary>
    public async Task<object?> GetTokenInfo([Service] TokenManagerService tokenManager)
    {
        return await tokenManager.GetTokenInfoAsync();
    }
}

public class Mutation
{
    /// <summary>
    /// Send a template message via WhatsApp
    /// </summary>
    public async Task<SendMessageResponse> SendTemplateMessage(
        string to,
        string templateName,
        string? language,
        List<string>? parameters,
        [Service] WhatsAppService whatsAppService)
    {
        return await whatsAppService.SendTemplateMessageAsync(to, templateName, language, parameters);
    }

    /// <summary>
    /// Send a text message via WhatsApp
    /// </summary>
    public async Task<SendMessageResponse> SendTextMessage(
        string to,
        string text,
        [Service] WhatsAppService whatsAppService)
    {
        return await whatsAppService.SendTextMessageAsync(to, text);
    }

    /// <summary>
    /// Clear the token cache (useful for debugging)
    /// </summary>
    public string ClearTokenCache([Service] TokenManagerService tokenManager)
    {
        tokenManager.ClearTokenCache();
        return "Token cache cleared successfully";
    }
}
