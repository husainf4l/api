using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuthService.Pages.ApiKeys;

public class IndexModel : PageModel
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly ApiKeyService _apiKeyService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationService applicationService,
        UserService userService,
        ApiKeyService apiKeyService,
        ILogger<IndexModel> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    // Properties for the view
    public SelectList Applications { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
    public string? SelectedApplicationCode { get; set; }
    public int TotalKeys { get; set; }
    public int ActiveKeys { get; set; }
    public int RevokedKeys { get; set; }
    public int ExpiredKeys { get; set; }
    public List<ApiKeyViewModel> ApiKeys { get; set; } = new();
    public string? NewlyGeneratedKey { get; set; }
    public string? NewlyGeneratedKeyName { get; set; }
    public List<string> NewlyGeneratedScopes { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public CreateApiKeyRequest CreateRequest { get; set; } = new();

    public async Task OnGetAsync(string? app, string? success, string? error)
    {
        SuccessMessage = success;
        ErrorMessage = error;

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

            // Load API keys if application is selected
            if (!string.IsNullOrEmpty(SelectedApplicationCode))
            {
                var application = await _applicationService.GetApplicationByCodeAsync(SelectedApplicationCode);
                if (application != null)
                {
                    await LoadApiKeysAsync(application.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API keys page");
            ErrorMessage = "Failed to load API keys";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload data and show validation errors
                await ReloadDataAsync();
                return Page();
            }

            if (string.IsNullOrEmpty(SelectedApplicationCode))
            {
                ErrorMessage = "Please select an application";
                await ReloadDataAsync();
                return Page();
            }

            var application = await _applicationService.GetApplicationByCodeAsync(SelectedApplicationCode);
            if (application == null)
            {
                ErrorMessage = "Selected application not found";
                await ReloadDataAsync();
                return Page();
            }

            // Get the first user from the application (in a real app, this should be the authenticated user)
            var users = await _userService.GetUsersByApplicationAsync(application.Id, 1, 1);
            if (!users.Any())
            {
                ErrorMessage = "No users found in the selected application";
                await ReloadDataAsync();
                return Page();
            }

            var result = await _apiKeyService.CreateApiKeyAsync(CreateRequest, application.Id, users.First().Id);
            if (result == null)
            {
                ErrorMessage = "Failed to create API key. Key name may already exist.";
                await ReloadDataAsync();
                return Page();
            }

            // Store the generated key for display (only shown once)
            NewlyGeneratedKey = result.ApiKey;
            NewlyGeneratedKeyName = result.ApiKeyEntity.Name;
            NewlyGeneratedScopes = result.ApiKeyEntity.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            _logger.LogInformation("API key {KeyName} created via dashboard for application {AppCode}",
                CreateRequest.Name, SelectedApplicationCode);

            // Reload data to show the new key in the list
            await ReloadDataAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            ErrorMessage = "Failed to create API key";
            await ReloadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id)
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedApplicationCode))
            {
                ErrorMessage = "Application not selected";
                return RedirectToPage();
            }

            // Get the first user (should be authenticated user in real app)
            var application = await _applicationService.GetApplicationByCodeAsync(SelectedApplicationCode);
            if (application == null)
            {
                ErrorMessage = "Application not found";
                return RedirectToPage();
            }

            var users = await _userService.GetUsersByApplicationAsync(application.Id, 1, 1);
            if (!users.Any())
            {
                ErrorMessage = "No users found";
                return RedirectToPage();
            }

            var success = await _apiKeyService.RevokeApiKeyAsync(id, users.First().Id);
            if (!success)
            {
                ErrorMessage = "Failed to revoke API key";
                return RedirectToPage();
            }

            _logger.LogInformation("API key {ApiKeyId} revoked via dashboard", id);
            return RedirectToPage(new { app = SelectedApplicationCode, success = "API key revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
            return RedirectToPage(new { app = SelectedApplicationCode, error = "Failed to revoke API key" });
        }
    }

    private async Task LoadApiKeysAsync(Guid applicationId)
    {
        var apiKeys = await _apiKeyService.GetApiKeysByApplicationAsync(applicationId, 1, 100);

        ApiKeys = apiKeys.Select(ak => new ApiKeyViewModel
        {
            Id = ak.Id,
            Name = ak.Name,
            Description = ak.Description,
            KeyPrefix = ak.Name.Length > 8 ? ak.Name.Substring(0, 8) + "..." : ak.Name,
            Scopes = ak.Scopes,
            ApplicationName = ak.ApplicationName,
            Environment = "development", // TODO: Add environment field to API key
            LastUsedAt = ak.LastUsedAt,
            LastUsedIp = null, // TODO: Track last used IP
            UsageCount = 0, // TODO: Implement usage tracking
            IsRevoked = ak.IsRevoked,
            IsActive = ak.IsActive,
            ExpiresAt = ak.ExpiresAt
        }).ToList();

        TotalKeys = await _apiKeyService.GetApiKeyCountAsync(applicationId);
        ActiveKeys = await _apiKeyService.GetActiveApiKeyCountAsync(applicationId);
        RevokedKeys = TotalKeys - ActiveKeys;
        ExpiredKeys = ApiKeys.Count(ak => ak.ExpiresAt.HasValue && ak.ExpiresAt.Value < DateTime.UtcNow);
    }

    private async Task ReloadDataAsync()
    {
        if (!string.IsNullOrEmpty(SelectedApplicationCode))
        {
            var application = await _applicationService.GetApplicationByCodeAsync(SelectedApplicationCode);
            if (application != null)
            {
                await LoadApiKeysAsync(application.Id);
            }
        }
    }
}

public class ApiKeyViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public string? ApplicationName { get; set; }
    public string Environment { get; set; } = "development";
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public int UsageCount { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}
