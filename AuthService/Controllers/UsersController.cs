using AuthService.Models.DTOs;
using AuthService.Services;
using AuthService.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("admin/{appCode}/users")]
[Authorize] // Require authentication
[RequireRole("Admin")] // Require Admin role
public class UsersController : ControllerBase
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly RoleService _roleService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationService applicationService,
        UserService userService,
        RoleService roleService,
        ILogger<UsersController> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(string appCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            List<UserDto> users;
            if (!string.IsNullOrEmpty(search))
            {
                // Convert User entities to UserDto for search results
                var userEntities = await _userService.SearchUsersAsync(application.Id, search, page, pageSize);
                users = new List<UserDto>();
                foreach (var user in userEntities)
                {
                    var userDto = await _userService.GetUserDtoAsync(user.Id);
                    if (userDto != null)
                    {
                        users.Add(userDto);
                    }
                }
            }
            else
            {
                users = await _userService.GetUsersWithRolesAsync(application.Id, page, pageSize);
            }

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    total = await _userService.GetTotalUsersCountAsync(application.Id),
                    search
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for application {AppCode}", appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string appCode, Guid id)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            var user = await _userService.GetUserDtoAsync(id);
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            return Ok(new
            {
                success = true,
                data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} for application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string appCode, Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            var updatedUser = await _userService.UpdateUserAsync(id, request);
            if (updatedUser == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to update user"
                });
            }

            _logger.LogInformation("User {UserId} updated in application {AppCode}", id, appCode);

            return Ok(new
            {
                success = true,
                message = "User updated successfully",
                data = updatedUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} for application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during user update"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string appCode, Guid id)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            var success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to delete user"
                });
            }

            _logger.LogInformation("User {UserId} deleted from application {AppCode}", id, appCode);

            return Ok(new
            {
                success = true,
                message = "User deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} from application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during user deletion"
            });
        }
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRoleToUser(string appCode, Guid id, [FromBody] AssignRoleRequest request)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            // Get role by name for this application
            var role = await _roleService.GetRoleByNameAsync(application.Id, request.RoleName);
            if (role == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Role '{request.RoleName}' not found in this application"
                });
            }

            var success = await _userService.AssignRoleToUserAsync(id, role.Id);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to assign role to user"
                });
            }

            _logger.LogInformation("Role {RoleName} assigned to user {UserId} in application {AppCode}",
                request.RoleName, id, appCode);

            return Ok(new
            {
                success = true,
                message = $"Role '{request.RoleName}' assigned to user successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId} in application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during role assignment"
            });
        }
    }

    [HttpDelete("{id}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRoleFromUser(string appCode, Guid id, string roleName)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            // Get role by name for this application
            var role = await _roleService.GetRoleByNameAsync(application.Id, roleName);
            if (role == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Role '{roleName}' not found in this application"
                });
            }

            var success = await _userService.RemoveRoleFromUserAsync(id, role.Id);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to remove role from user"
                });
            }

            _logger.LogInformation("Role {RoleName} removed from user {UserId} in application {AppCode}",
                roleName, id, appCode);

            return Ok(new
            {
                success = true,
                message = $"Role '{roleName}' removed from user successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId} in application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during role removal"
            });
        }
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetUserRoles(string appCode, Guid id)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            var roles = await _userService.GetUserRolesAsync(id);

            return Ok(new
            {
                success = true,
                data = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId} in application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet("{id}/sessions")]
    public async Task<IActionResult> GetUserSessions(string appCode, Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get application by code
            var application = await _applicationService.GetApplicationByCodeAsync(appCode);
            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found"
                });
            }

            // Verify user belongs to the application
            var userEntity = await _userService.GetUserByIdAsync(id);
            if (userEntity?.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found in this application"
                });
            }

            // TODO: Implement session retrieval from SessionLogs
            // For now, return empty array
            var sessions = new List<object>();

            return Ok(new
            {
                success = true,
                data = sessions,
                pagination = new
                {
                    page,
                    pageSize,
                    total = 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user {UserId} in application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }
}

public class AssignRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}
