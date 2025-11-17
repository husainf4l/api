using AuthService.DTOs;
using AuthService.Middleware;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyService apiKeyService,
        IApiKeyRepository apiKeyRepository,
        IUserRepository userRepository,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _apiKeyRepository = apiKeyRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new API key
    /// POST /api/apikeys/generate
    /// </summary>
    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult<CreateApiKeyResponse>> GenerateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "API key name is required" });
        }

        if (request.Scopes == null || request.Scopes.Count == 0)
        {
            return BadRequest(new { message = "At least one scope is required" });
        }

        try
        {
            DateTime? expiresAt = null;
            if (request.ExpiresInDays.HasValue)
            {
                expiresAt = DateTime.UtcNow.AddDays(request.ExpiresInDays.Value);
            }

            var (apiKey, plainTextKey) = await _apiKeyService.GenerateApiKeyAsync(
                userId.Value,
                request.Name,
                request.Scopes,
                request.Description,
                request.ApplicationId,
                expiresAt,
                request.Environment,
                request.RateLimitPerHour,
                request.RateLimitPerDay
            );

            var response = new CreateApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                KeyPrefix = apiKey.KeyPrefix,
                ApiKey = plainTextKey, // ONLY shown this one time!
                Scopes = apiKey.Scopes,
                Environment = apiKey.Environment ?? "production",
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key");
            return StatusCode(500, new { message = "An error occurred while generating the API key" });
        }
    }

    /// <summary>
    /// Validate an API key (used by other microservices)
    /// POST /api/apikeys/validate
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ValidateApiKeyResponse>> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new { message = "API key is required" });
        }

        try
        {
            var apiKey = await _apiKeyService.ValidateApiKeyAsync(request.ApiKey);

            if (apiKey == null)
            {
                return Ok(new ValidateApiKeyResponse
                {
                    IsValid = false,
                    Message = "Invalid or expired API key"
                });
            }

            // Update last used
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _apiKeyService.UpdateLastUsedAsync(apiKey.Id, ipAddress);

            var response = new ValidateApiKeyResponse
            {
                IsValid = true,
                UserId = apiKey.UserId,
                UserEmail = apiKey.User?.Email,
                ApplicationId = apiKey.ApplicationId,
                ApplicationName = apiKey.Application?.Name,
                Scopes = apiKey.Scopes,
                Message = "API key is valid"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return StatusCode(500, new { message = "An error occurred while validating the API key" });
        }
    }

    /// <summary>
    /// List all API keys for the authenticated user
    /// GET /api/apikeys
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetApiKeys([FromQuery] bool includeRevoked = false)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId.Value, includeRevoked);

            var response = apiKeys.Select(k => new ApiKeyResponse
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                KeyPrefix = k.KeyPrefix,
                Scopes = k.Scopes,
                ApplicationName = k.Application?.Name,
                Environment = k.Environment ?? "production",
                IsActive = k.IsActive,
                IsRevoked = k.IsRevoked,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                LastUsedIp = k.LastUsedIp,
                UsageCount = k.UsageCount,
                CreatedAt = k.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return StatusCode(500, new { message = "An error occurred while retrieving API keys" });
        }
    }

    /// <summary>
    /// Get a specific API key by ID
    /// GET /api/apikeys/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiKeyResponse>> GetApiKey(Guid id)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);

            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Check ownership
            if (apiKey.UserId != userId.Value)
            {
                return Forbid();
            }

            var response = new ApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Description = apiKey.Description,
                KeyPrefix = apiKey.KeyPrefix,
                Scopes = apiKey.Scopes,
                ApplicationName = apiKey.Application?.Name,
                Environment = apiKey.Environment ?? "production",
                IsActive = apiKey.IsActive,
                IsRevoked = apiKey.IsRevoked,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                LastUsedIp = apiKey.LastUsedIp,
                UsageCount = apiKey.UsageCount,
                CreatedAt = apiKey.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key {KeyId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the API key" });
        }
    }

    /// <summary>
    /// Revoke an API key
    /// POST /api/apikeys/{id}/revoke
    /// </summary>
    [HttpPost("{id}/revoke")]
    [Authorize]
    public async Task<ActionResult> RevokeApiKey(Guid id, [FromBody] RevokeApiKeyRequest request)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);

            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Check ownership
            if (apiKey.UserId != userId.Value)
            {
                return Forbid();
            }

            if (apiKey.IsRevoked)
            {
                return BadRequest(new { message = "API key is already revoked" });
            }

            var result = await _apiKeyService.RevokeApiKeyAsync(id, request.Reason);

            if (result)
            {
                return Ok(new { message = "API key revoked successfully" });
            }

            return StatusCode(500, new { message = "Failed to revoke API key" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {KeyId}", id);
            return StatusCode(500, new { message = "An error occurred while revoking the API key" });
        }
    }

    /// <summary>
    /// Delete an API key permanently
    /// DELETE /api/apikeys/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteApiKey(Guid id)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);

            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            // Check ownership
            if (apiKey.UserId != userId.Value)
            {
                return Forbid();
            }

            var result = await _apiKeyRepository.DeleteAsync(id);

            if (result)
            {
                return Ok(new { message = "API key deleted successfully" });
            }

            return StatusCode(500, new { message = "Failed to delete API key" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key {KeyId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the API key" });
        }
    }

    /// <summary>
    /// Get all API keys (Admin only)
    /// GET /api/apikeys/admin/all
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetAllApiKeys([FromQuery] bool includeRevoked = false)
    {
        // TODO: Add admin role check here
        
        try
        {
            var apiKeys = await _apiKeyRepository.GetAllAsync(includeRevoked);

            var response = apiKeys.Select(k => new ApiKeyResponse
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                KeyPrefix = k.KeyPrefix,
                Scopes = k.Scopes,
                ApplicationName = k.Application?.Name,
                Environment = k.Environment ?? "production",
                IsActive = k.IsActive,
                IsRevoked = k.IsRevoked,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                LastUsedIp = k.LastUsedIp,
                UsageCount = k.UsageCount,
                CreatedAt = k.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all API keys");
            return StatusCode(500, new { message = "An error occurred while retrieving API keys" });
        }
    }
}
