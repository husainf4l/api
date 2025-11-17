using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("admin/{appCode}/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly ApplicationService _applicationService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleService roleService,
        ApplicationService applicationService,
        ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _applicationService = applicationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles for an application
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles(string appCode)
    {
        try
        {
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var roles = await _roleService.GetRolesByApplicationAsync(application.Id);

            return Ok(new
            {
                success = true,
                data = roles.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    description = r.Description,
                    applicationId = r.ApplicationId
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for application {AppCode}", appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Create a new role for an application
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole(string appCode, [FromBody] CreateRoleRequest request)
    {
        try
        {
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var role = await _roleService.CreateRoleAsync(application.Id, request.Name, request.Description);
            if (role == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create role. Role name might already exist."
                });
            }

            _logger.LogInformation("Role {RoleName} created for application {AppCode}", request.Name, appCode);

            return Ok(new
            {
                success = true,
                message = "Role created successfully",
                data = new
                {
                    id = role.Id,
                    name = role.Name,
                    description = role.Description,
                    applicationId = role.ApplicationId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role for application {AppCode}", appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Update a role
    /// </summary>
    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(string appCode, Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var success = await _roleService.UpdateRoleAsync(roleId, request.Name, request.Description);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to update role"
                });
            }

            _logger.LogInformation("Role {RoleId} updated for application {AppCode}", roleId, appCode);

            return Ok(new
            {
                success = true,
                message = "Role updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId} for application {AppCode}", roleId, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(string appCode, Guid roleId)
    {
        try
        {
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var success = await _roleService.DeleteRoleAsync(roleId);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to delete role. Role might not exist or is a system role."
                });
            }

            _logger.LogInformation("Role {RoleId} deleted for application {AppCode}", roleId, appCode);

            return Ok(new
            {
                success = true,
                message = "Role deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId} for application {AppCode}", roleId, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
