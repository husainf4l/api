using AuthService.Models.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("admin/{appCode}/api-keys")]
[Authorize] // Require authentication for all admin endpoints
public class ApiKeysController : ControllerBase
{
    private readonly ApplicationService _applicationService;
    private readonly UserService _userService;
    private readonly ApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        ApplicationService applicationService,
        UserService userService,
        ApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _applicationService = applicationService;
        _userService = userService;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetApiKeys(string appCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
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

            var apiKeys = await _apiKeyService.GetApiKeysByApplicationAsync(application.Id, page, pageSize);
            var totalCount = await _apiKeyService.GetApiKeyCountAsync(application.Id);

            return Ok(new
            {
                success = true,
                data = apiKeys,
                pagination = new
                {
                    page,
                    pageSize,
                    total = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys for application {AppCode}", appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKey(string appCode, Guid id)
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

            var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null || apiKey.ApplicationId != application.Id)
            {
                return NotFound(new
                {
                    success = false,
                    message = "API key not found in this application"
                });
            }

            return Ok(new
            {
                success = true,
                data = apiKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key {ApiKeyId} for application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey(string appCode, [FromBody] CreateApiKeyRequest request)
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

            // For now, we'll use a default user. In a real app, this should come from the authenticated user
            // TODO: Get the actual authenticated user
            var defaultUser = await _userService.GetUsersByApplicationAsync(application.Id, 1, 1);
            if (!defaultUser.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No users found in this application"
                });
            }

            var result = await _apiKeyService.CreateApiKeyAsync(request, application.Id, defaultUser.First().Id);
            if (result == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create API key. Key name may already exist."
                });
            }

            _logger.LogInformation("API key {KeyName} created for application {AppCode}", request.Name, appCode);

            return CreatedAtAction(nameof(GetApiKey), new { appCode, id = result.ApiKeyEntity.Id }, new
            {
                success = true,
                message = "API key created successfully",
                data = new
                {
                    id = result.ApiKeyEntity.Id,
                    name = result.ApiKeyEntity.Name,
                    apiKey = result.ApiKey, // Only show this once!
                    scopes = result.ApiKeyEntity.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    expiresAt = result.ApiKeyEntity.ExpiresAt,
                    createdAt = result.ApiKeyEntity.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key for application {AppCode}", appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during API key creation"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(string appCode, Guid id, [FromBody] UpdateApiKeyRequest request)
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

            // For now, use default user. TODO: Get actual authenticated user
            var defaultUser = await _userService.GetUsersByApplicationAsync(application.Id, 1, 1);
            if (!defaultUser.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No users found in this application"
                });
            }

            var success = await _apiKeyService.UpdateApiKeyAsync(id, request, defaultUser.First().Id);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to update API key"
                });
            }

            return Ok(new
            {
                success = true,
                message = "API key updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key {ApiKeyId} for application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during API key update"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(string appCode, Guid id)
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

            // For now, use default user. TODO: Get actual authenticated user
            var defaultUser = await _userService.GetUsersByApplicationAsync(application.Id, 1, 1);
            if (!defaultUser.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No users found in this application"
                });
            }

            var success = await _apiKeyService.RevokeApiKeyAsync(id, defaultUser.First().Id);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to revoke API key"
                });
            }

            _logger.LogInformation("API key {ApiKeyId} revoked for application {AppCode}", id, appCode);

            return Ok(new
            {
                success = true,
                message = "API key revoked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId} for application {AppCode}", id, appCode);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during API key revocation"
            });
        }
    }
}

// Separate controller for internal API key validation (no auth required)
[ApiController]
[Route("internal")]
public class ApiKeyValidationController : ControllerBase
{
    private readonly ApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyValidationController> _logger;

    public ApiKeyValidationController(ApiKeyService apiKeyService, ILogger<ApiKeyValidationController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    [HttpPost("validate-api-key")]
    public async Task<IActionResult> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _apiKeyService.ValidateApiKeyAsync(request.ApiKey, request.RequestedScope, clientIp);

            if (!result.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "API key is valid",
                data = new
                {
                    applicationId = result.ApplicationId,
                    userId = result.UserId,
                    scopes = result.Scopes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during API key validation"
            });
        }
    }
}
