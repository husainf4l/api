using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthService.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly RoleService _roleService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationService applicationService,
        UserService userService,
        RoleService roleService,
        ILogger<DetailsModel> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _roleService = roleService;
        _logger = logger;
    }

    public new UserDto? User { get; set; }
    public ApplicationDto? Application { get; set; }
    public List<string> AvailableRoles { get; set; } = new();
    public string? UserRole { get; set; }
    public List<SessionLogDto> Sessions { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string ActiveTab { get; set; } = "profile";

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? App { get; set; }

    [BindProperty]
    public UpdateUserRequest UpdateRequest { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? tab)
    {
        ActiveTab = tab ?? "profile";

        try
        {
            // Get user
            User = await _userService.GetUserDtoAsync(Id);
            if (User == null)
            {
                return NotFound();
            }

            // Get application
            if (!string.IsNullOrEmpty(App))
            {
                var appEntity = await _applicationService.GetApplicationByCodeAsync(App);
                if (appEntity != null)
                {
                    Application = new ApplicationDto
                    {
                        Id = appEntity.Id,
                        Name = appEntity.Name,
                        Code = appEntity.Code,
                        CreatedAt = appEntity.CreatedAt,
                        IsActive = appEntity.IsActive
                    };
                }
            }

            // Load data based on active tab
            if (ActiveTab == "roles")
            {
                await LoadRolesDataAsync();
            }
            else if (ActiveTab == "sessions")
            {
                await LoadSessionsDataAsync();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for {UserId}", Id);
            ErrorMessage = "Failed to load user details";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await LoadRolesDataAsync(); // Reload roles data for the roles tab
                return Page();
            }

            var updatedUser = await _userService.UpdateUserAsync(Id, UpdateRequest);
            if (updatedUser == null)
            {
                ErrorMessage = "Failed to update user profile";
                await LoadRolesDataAsync();
                return Page();
            }

            User = updatedUser;
            SuccessMessage = "User profile updated successfully";

            return RedirectToPage(new { id = Id, app = App, tab = "profile", success = SuccessMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile {UserId}", Id);
            ErrorMessage = "Failed to update user profile";
            await LoadRolesDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(string roleName)
    {
        try
        {
            if (Application == null)
            {
                ErrorMessage = "Application not found";
                await LoadRolesDataAsync();
                return Page();
            }

            // Get role by name
            var role = await _roleService.GetRoleByNameAsync(Application.Id, roleName);
            if (role == null)
            {
                ErrorMessage = $"Role '{roleName}' not found";
                await LoadRolesDataAsync();
                return Page();
            }

            var success = await _userService.AssignRoleToUserAsync(Id, role.Id);
            if (!success)
            {
                ErrorMessage = "Failed to assign role";
                await LoadRolesDataAsync();
                return Page();
            }

            SuccessMessage = $"Role '{roleName}' assigned successfully";
            return RedirectToPage(new { id = Id, app = App, tab = "roles", success = SuccessMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", Id);
            ErrorMessage = "Failed to assign role";
            await LoadRolesDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(string roleName)
    {
        try
        {
            if (Application == null)
            {
                ErrorMessage = "Application not found";
                await LoadRolesDataAsync();
                return Page();
            }

            // Get role by name
            var role = await _roleService.GetRoleByNameAsync(Application.Id, roleName);
            if (role == null)
            {
                ErrorMessage = $"Role '{roleName}' not found";
                await LoadRolesDataAsync();
                return Page();
            }

            var success = await _userService.RemoveRoleFromUserAsync(Id, role.Id);
            if (!success)
            {
                ErrorMessage = "Failed to remove role";
                await LoadRolesDataAsync();
                return Page();
            }

            SuccessMessage = $"Role '{roleName}' removed successfully";
            return RedirectToPage(new { id = Id, app = App, tab = "roles", success = SuccessMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", Id);
            ErrorMessage = "Failed to remove role";
            await LoadRolesDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync()
    {
        try
        {
            var updateRequest = new UpdateUserRequest
            {
                IsActive = User?.IsActive == false
            };

            var updatedUser = await _userService.UpdateUserAsync(Id, updateRequest);
            if (updatedUser == null)
            {
                ErrorMessage = "Failed to update user status";
                return Page();
            }

            User = updatedUser;
            SuccessMessage = $"User {(User.IsActive ? "activated" : "deactivated")} successfully";

            return RedirectToPage(new { id = Id, app = App, tab = "profile", success = SuccessMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status {UserId}", Id);
            ErrorMessage = "Failed to update user status";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(App))
            {
                var success = await _userService.DeleteUserAsync(Id);
                if (!success)
                {
                    ErrorMessage = "Failed to delete user";
                    return Page();
                }

                _logger.LogInformation("User {UserId} deleted from application {AppCode}", Id, App);
                return RedirectToPage("/Users/Index", new { app = App, success = "User deleted successfully" });
            }

            ErrorMessage = "Application code is required";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", Id);
            ErrorMessage = "Failed to delete user";
            return Page();
        }
    }

    private async Task LoadRolesDataAsync()
    {
        if (Application == null) return;

        UserRole = await _userService.GetUserRolesAsync(Id);
        var allRoles = await _roleService.GetRolesByApplicationAsync(Application.Id);
        AvailableRoles = allRoles
            .Where(r => UserRole == null || r.Name != UserRole)
            .Select(r => r.Name)
            .ToList();
    }

    private async Task LoadSessionsDataAsync()
    {
        // TODO: Implement session loading when SessionLog service is available
        Sessions = new List<SessionLogDto>();
    }
}

public class SessionLogDto
{
    public Guid Id { get; set; }
    public DateTime? LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; }
}
