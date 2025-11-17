using WhatsApp.Models;
using WhatsApp.Services;

namespace WhatsApp.GraphQL;

public class Query
{
    /// <summary>
    /// Get all available template options (for dropdown/UI)
    /// </summary>
    public List<TemplateInfo> GetAvailableTemplates()
    {
        return TemplateHelper.GetAllTemplates();
    }

    /// <summary>
    /// Get information about a specific template
    /// </summary>
    public TemplateInfo GetTemplateInfo(TemplateType templateType)
    {
        return TemplateHelper.GetTemplateInfo(templateType);
    }

    /// <summary>
    /// List all WhatsApp message templates from API
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
    /// Send a template message via WhatsApp using template type
    /// </summary>
    public async Task<SendMessageResponse> SendTemplate(
        string to,
        TemplateType templateType,
        List<string>? parameters,
        [Service] WhatsAppService whatsAppService)
    {
        var templateInfo = TemplateHelper.GetTemplateInfo(templateType);
        return await whatsAppService.SendTemplateMessageAsync(
            to, 
            templateInfo.Name, 
            templateInfo.Language, 
            parameters
        );
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
