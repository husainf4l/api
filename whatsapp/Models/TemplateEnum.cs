namespace WhatsApp.Models;

/// <summary>
/// Available WhatsApp message templates
/// </summary>
public enum TemplateType
{
    HelloWorld,
    TempPassword,
    CvReceivedNotification,
    ScoreReport
}

public static class TemplateHelper
{
    private static readonly Dictionary<TemplateType, TemplateInfo> TemplateMap = new()
    {
        {
            TemplateType.HelloWorld,
            new TemplateInfo
            {
                Name = "hello_world",
                DisplayName = "Hello World",
                Language = "en_US",
                Description = "Simple hello world template",
                RequiresParameters = false,
                ParameterCount = 0
            }
        },
        {
            TemplateType.TempPassword,
            new TemplateInfo
            {
                Name = "temppassword",
                DisplayName = "Temporary Password",
                Language = "en_US",
                Description = "Send temporary password with button",
                RequiresParameters = true,
                ParameterCount = 1,
                ParameterDescriptions = new[] { "Password/Code" }
            }
        },
        {
            TemplateType.CvReceivedNotification,
            new TemplateInfo
            {
                Name = "cv_received_notification",
                DisplayName = "CV Received Notification",
                Language = "en",
                Description = "Notify candidate that CV was received",
                RequiresParameters = true,
                ParameterCount = 1,
                ParameterDescriptions = new[] { "Candidate Name" }
            }
        },
        {
            TemplateType.ScoreReport,
            new TemplateInfo
            {
                Name = "score_report",
                DisplayName = "Score Report",
                Language = "en",
                Description = "Send score report to user",
                RequiresParameters = true,
                ParameterCount = 1,
                ParameterDescriptions = new[] { "Score/Result" }
            }
        }
    };

    public static string GetTemplateName(TemplateType type) => TemplateMap[type].Name;
    
    public static string GetLanguage(TemplateType type) => TemplateMap[type].Language;
    
    public static TemplateInfo GetTemplateInfo(TemplateType type) => TemplateMap[type];
    
    public static List<TemplateInfo> GetAllTemplates() => 
        TemplateMap.Select(kvp => kvp.Value with { Type = kvp.Key }).ToList();
}

public record TemplateInfo
{
    public TemplateType? Type { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Language { get; init; }
    public required string Description { get; init; }
    public bool RequiresParameters { get; init; }
    public int ParameterCount { get; init; }
    public string[]? ParameterDescriptions { get; init; }
}
