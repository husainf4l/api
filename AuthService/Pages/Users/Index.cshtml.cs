using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuthService.Pages.Users;

public class IndexModel : PageModel
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationService applicationService,
        UserService userService,
        ILogger<IndexModel> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _logger = logger;
    }

    public List<UserDto> Users { get; set; } = new();
    public SelectList Applications { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
    public string? SelectedApplicationCode { get; set; }
    public string? SearchTerm { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalUsers { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalUsers / PageSize);
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(
        string? app,
        string? search,
        int page = 1,
        string? success = null,
        string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;
        CurrentPage = Math.Max(1, page);
        SearchTerm = search;

        try
        {
            // Load applications for dropdown
            var applications = await _applicationService.GetAllApplicationsAsync();
            Applications = new SelectList(
                applications.Select(a => new SelectListItem
                {
                    Value = a.Code,
                    Text = $"{a.Name} ({a.Code})",
                    Selected = a.Code == app
                }),
                "Value",
                "Text");

            // If no application selected, show first one
            if (string.IsNullOrEmpty(app) && applications.Any())
            {
                SelectedApplicationCode = applications.First().Code;
            }
            else
            {
                SelectedApplicationCode = app;
            }

            // Load users if application is selected
            if (!string.IsNullOrEmpty(SelectedApplicationCode))
            {
                var application = await _applicationService.GetApplicationByCodeAsync(SelectedApplicationCode);
                if (application != null)
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        // Search functionality
                        var userEntities = await _userService.SearchUsersAsync(application.Id, search, CurrentPage, PageSize);
                        Users = new List<UserDto>();
                        foreach (var user in userEntities)
                        {
                            var userDto = await _userService.GetUserDtoAsync(user.Id);
                            if (userDto != null)
                            {
                                Users.Add(userDto);
                            }
                        }
                        TotalUsers = Users.Count; // Approximate for search
                    }
                    else
                    {
                        Users = await _userService.GetUsersWithRolesAsync(application.Id, CurrentPage, PageSize);
                        TotalUsers = await _userService.GetTotalUsersCountAsync(application.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            ErrorMessage = "Failed to load users";
            Users = new List<UserDto>();
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, string app)
    {
        try
        {
            // TODO: Implement toggle user status functionality
            return RedirectToPage(new { app, error = "Toggle user status not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status {UserId}", id);
            return RedirectToPage(new { app, error = "Failed to update user status" });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string app)
    {
        try
        {
            // TODO: Implement delete user functionality
            return RedirectToPage(new { app, error = "Delete user not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return RedirectToPage(new { app, error = "Failed to delete user" });
        }
    }
}
