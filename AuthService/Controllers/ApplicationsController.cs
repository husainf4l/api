using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationRepository applicationRepository,
        IUserSessionRepository sessionRepository,
        IPasswordService passwordService,
        ILogger<ApplicationsController> logger)
    {
        _applicationRepository = applicationRepository;
        _sessionRepository = sessionRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new application
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApplicationResponse>> RegisterApplication([FromBody] RegisterApplicationRequest request)
    {
        try
        {
            // Check if client ID already exists
            var existing = await _applicationRepository.GetByClientIdAsync(request.ClientId);
            if (existing != null)
            {
                return BadRequest(new { message = "Client ID already exists" });
            }

            // Hash the client secret
            var hashedSecret = _passwordService.HashPassword(request.ClientSecret);

            var application = new Application
            {
                Name = request.Name,
                ClientId = request.ClientId,
                ClientSecret = hashedSecret,
                Description = request.Description,
                AllowedOrigins = request.AllowedOrigins ?? new List<string>(),
                AllowedScopes = request.AllowedScopes ?? new List<string>()
            };

            await _applicationRepository.CreateAsync(application);

            _logger.LogInformation("Application registered: {ClientId}", request.ClientId);

            return Ok(new ApplicationResponse
            {
                Id = application.Id,
                Name = application.Name,
                ClientId = application.ClientId,
                Description = application.Description,
                AllowedOrigins = application.AllowedOrigins,
                AllowedScopes = application.AllowedScopes,
                IsActive = application.IsActive,
                CreatedAt = application.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering application");
            return StatusCode(500, new { message = "An error occurred while registering the application" });
        }
    }

    /// <summary>
    /// Get all registered applications
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ApplicationResponse>>> GetApplications()
    {
        try
        {
            var applications = await _applicationRepository.GetAllAsync();
            var response = applications.Select(a => new ApplicationResponse
            {
                Id = a.Id,
                Name = a.Name,
                ClientId = a.ClientId,
                Description = a.Description,
                AllowedOrigins = a.AllowedOrigins,
                AllowedScopes = a.AllowedScopes,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching applications");
            return StatusCode(500, new { message = "An error occurred while fetching applications" });
        }
    }

    /// <summary>
    /// Get active sessions for an application
    /// </summary>
    [HttpGet("{applicationId}/sessions")]
    public async Task<ActionResult<List<SessionResponse>>> GetApplicationSessions(Guid applicationId)
    {
        try
        {
            var sessions = await _sessionRepository.GetApplicationSessionsAsync(applicationId);
            var response = sessions.Select(s => new SessionResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                UserEmail = s.User.Email,
                ApplicationName = s.Application.Name,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                DeviceInfo = s.DeviceInfo,
                LoginAt = s.LoginAt,
                LastActivityAt = s.LastActivityAt,
                IsActive = s.IsActive
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching application sessions");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}

public class RegisterApplicationRequest
{
    public required string Name { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public string? Description { get; set; }
    public List<string>? AllowedOrigins { get; set; }
    public List<string>? AllowedScopes { get; set; }
}

public class ApplicationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedScopes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}
