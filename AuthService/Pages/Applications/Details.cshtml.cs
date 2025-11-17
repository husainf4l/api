using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthService.Pages.Applications;

public class DetailsModel : PageModel
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationService applicationService,
        UserService userService,
        ILogger<DetailsModel> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _logger = logger;
    }

    public ApplicationDto? Application { get; set; }
    public List<UserDto> Users { get; set; } = new();
    public List<ApiKeyDto> ApiKeys { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string ActiveTab { get; set; } = "overview";

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync(string? tab)
    {
        ActiveTab = tab ?? "overview";

        try
        {
            Application = await GetApplicationWithStatsAsync(Id);
            if (Application == null)
            {
                return NotFound();
            }

            // Load data based on active tab
            if (ActiveTab == "users")
            {
                Users = await _userService.GetUsersWithRolesAsync(Id, 1, 50);
            }
            else if (ActiveTab == "api-keys")
            {
                // TODO: Load API keys when API key service is implemented
                ApiKeys = new List<ApiKeyDto>();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application details for {ApplicationId}", Id);
            ErrorMessage = "Failed to load application details";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync()
    {
        try
        {
            // TODO: Implement toggle status functionality in ApplicationService
            return RedirectToPage(new { id = Id, tab = "overview", error = "Toggle status not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling application status {ApplicationId}", Id);
            return RedirectToPage(new { id = Id, tab = "overview", error = "Failed to update application status" });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            // TODO: Implement delete functionality in ApplicationService
            return RedirectToPage(new { id = Id, tab = "overview", error = "Delete application not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {ApplicationId}", Id);
            return RedirectToPage(new { id = Id, tab = "overview", error = "Failed to delete application" });
        }
    }

    public async Task<IActionResult> OnPostRegenerateSecretAsync()
    {
        try
        {
            // TODO: Implement regenerate client secret functionality
            return RedirectToPage(new { id = Id, tab = "settings", error = "Regenerate secret not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating client secret for application {ApplicationId}", Id);
            return RedirectToPage(new { id = Id, tab = "settings", error = "Failed to regenerate client secret" });
        }
    }

    private async Task<ApplicationDto?> GetApplicationWithStatsAsync(Guid applicationId)
    {
        var app = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (app == null) return null;

        return new ApplicationDto
        {
            Id = app.Id,
            Name = app.Name,
            Code = app.Code,
            CreatedAt = app.CreatedAt,
            IsActive = app.IsActive,
            UserCount = await _userService.GetTotalUsersCountAsync(applicationId),
            ApiKeyCount = 0, // TODO: Implement API key counting
            ActiveApiKeyCount = 0, // TODO: Implement active API key counting
            LastActivity = null // TODO: Implement last activity tracking
        };
    }
}
