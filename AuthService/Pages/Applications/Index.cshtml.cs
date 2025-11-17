using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthService.Pages.Applications;

public class IndexModel : PageModel
{
    private readonly ApplicationService _applicationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationService applicationService, ILogger<IndexModel> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    public List<ApplicationDto> Applications { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public CreateApplicationRequest CreateRequest { get; set; } = new();

    public async Task OnGetAsync(string? success, string? error)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        try
        {
            Applications = await _applicationService.GetAllApplicationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading applications");
            ErrorMessage = "Failed to load applications";
            Applications = new List<ApplicationDto>();
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload applications and show validation errors
                Applications = await _applicationService.GetAllApplicationsAsync();
                return Page();
            }

            var application = await _applicationService.CreateApplicationAsync(CreateRequest);

            if (application == null)
            {
                ErrorMessage = "Failed to create application. The code may already exist.";
                Applications = await _applicationService.GetAllApplicationsAsync();
                return Page();
            }

            _logger.LogInformation("Application created via dashboard: {ApplicationName} ({ApplicationCode})",
                application.Name, application.Code);

            return RedirectToPage(new { success = "Application created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            ErrorMessage = "Failed to create application";
            Applications = await _applicationService.GetAllApplicationsAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id)
    {
        try
        {
            // TODO: Implement toggle status functionality in ApplicationService
            // For now, just redirect back
            return RedirectToPage(new { error = "Toggle status not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling application status {ApplicationId}", id);
            return RedirectToPage(new { error = "Failed to update application status" });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            // TODO: Implement delete functionality in ApplicationService
            // For now, just redirect back
            return RedirectToPage(new { error = "Delete application not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {ApplicationId}", id);
            return RedirectToPage(new { error = "Failed to delete application" });
        }
    }
}
