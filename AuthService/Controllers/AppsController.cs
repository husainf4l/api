using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("admin/apps")]
[Authorize] // Require authentication for all admin endpoints
public class AppsController : ControllerBase
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly ILogger<AppsController> _logger;

    public AppsController(
        ApplicationService applicationService,
        UserService userService,
        ILogger<AppsController> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications()
    {
        try
        {
            var applications = await _applicationService.GetAllApplicationsAsync();

            return Ok(new
            {
                success = true,
                data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        try
        {
            var application = await _applicationService.GetApplicationByIdAsync(id);

            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Get additional statistics
            var totalUsers = await _userService.GetTotalUsersCountAsync(id);
            var activeUsers = await _userService.GetActiveUsersCountAsync(id);

            var result = new ApplicationDto
            {
                Id = application.Id,
                Name = application.Name,
                Code = application.Code,
                CreatedAt = application.CreatedAt,
                IsActive = application.IsActive,
                UserCount = totalUsers,
                ApiKeyCount = 0, // TODO: Implement API key counting
                ActiveApiKeyCount = 0, // TODO: Implement active API key counting
                LastActivity = null // TODO: Implement last activity tracking
            };

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application {ApplicationId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var application = await _applicationService.CreateApplicationAsync(request);

            if (application == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create application. Code may already exist."
                });
            }

            _logger.LogInformation("Application created: {ApplicationName} ({ApplicationCode})",
                application.Name, application.Code);

            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, new
            {
                success = true,
                message = "Application created successfully",
                data = application
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during application creation"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApplication(Guid id, [FromBody] UpdateApplicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // TODO: Implement application update logic in ApplicationService
            // For now, return not implemented
            return StatusCode(501, new
            {
                success = false,
                message = "Application update not yet implemented"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {ApplicationId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during application update"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApplication(Guid id)
    {
        try
        {
            // TODO: Implement application deletion logic in ApplicationService
            // For now, return not implemented
            return StatusCode(501, new
            {
                success = false,
                message = "Application deletion not yet implemented"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {ApplicationId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during application deletion"
            });
        }
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetApplicationUsers(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            // Verify application exists
            var application = await _applicationService.GetApplicationByIdAsync(id);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var users = await _userService.GetUsersWithRolesAsync(id, page, pageSize);

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    total = await _userService.GetTotalUsersCountAsync(id)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for application {ApplicationId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetApplicationStats(Guid id)
    {
        try
        {
            // Verify application exists
            var application = await _applicationService.GetApplicationByIdAsync(id);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var totalUsers = await _userService.GetTotalUsersCountAsync(id);
            var activeUsers = await _userService.GetActiveUsersCountAsync(id);

            var stats = new
            {
                applicationId = id,
                applicationName = application.Name,
                applicationCode = application.Code,
                totalUsers,
                activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                apiKeys = new
                {
                    total = 0, // TODO: Implement
                    active = 0, // TODO: Implement
                    expired = 0 // TODO: Implement
                },
                recentActivity = new
                {
                    loginsToday = 0, // TODO: Implement
                    registrationsThisWeek = 0 // TODO: Implement
                }
            };

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for application {ApplicationId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }
}

public class UpdateApplicationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
