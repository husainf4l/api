using System.Text.Json;
using AuthService.Models;

namespace AuthService.Services;

public class SiemAuditLogger : IAuditLogger
{
    private readonly ILogger<SiemAuditLogger> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _endpoint;
    private readonly string? _apiKey;

    public SiemAuditLogger(
        ILogger<SiemAuditLogger> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(SiemAuditLogger));
        _configuration = configuration;
        _endpoint = _configuration["Monitoring:Siem:Endpoint"]
            ?? throw new InvalidOperationException("Monitoring:Siem:Endpoint is not configured");
        _apiKey = _configuration["Monitoring:Siem:ApiKey"];
    }

    public async Task LogAsync(AuditEventType type, AuditEventContext context, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            eventType = type.ToString(),
            timestamp = DateTime.UtcNow,
            userId = context.UserId,
            email = context.Email,
            ip = context.IpAddress,
            device = context.DeviceInfo,
            extras = context.Extras
        };

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Audit event {EventType} for user {UserId} ip {Ip}", type, context.UserId, context.IpAddress);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("X-API-KEY", _apiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send audit event {EventType}: {StatusCode}", type, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audit event {EventType}", type);
        }
    }
}

