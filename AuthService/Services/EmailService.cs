using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace AuthService.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendVerificationEmailAsync(string toEmail, string verificationToken);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var emailServiceUrl = _configuration["EmailService:Url"] ?? "https://api.aqlaan.com/email/graphql";
            
            // GraphQL mutation for sending email
            var graphqlRequest = new
            {
                query = @"mutation SendEmail($input: SendEmailInput!) {
                    sendEmail(input: $input) {
                        success
                        message
                        messageId
                    }
                }",
                variables = new
                {
                    input = new
                    {
                        to = toEmail,
                        subject = subject,
                        body = body,
                        isHtml = true
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(graphqlRequest);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending email to {Email} via GraphQL endpoint {Url}", toEmail, emailServiceUrl);

            var response = await _httpClient.PostAsync(emailServiceUrl, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (result.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("sendEmail", out var sendEmail))
                {
                    var success = sendEmail.GetProperty("success").GetBoolean();
                    var message = sendEmail.GetProperty("message").GetString();
                    var messageId = sendEmail.TryGetProperty("messageId", out var msgId) ? msgId.GetString() : null;
                    
                    if (success)
                    {
                        _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", toEmail, messageId);
                    }
                    else
                    {
                        _logger.LogError("Failed to send email to {Email}. Message: {Message}", toEmail, message);
                    }
                }
                else if (result.TryGetProperty("errors", out var errors))
                {
                    _logger.LogError("GraphQL errors when sending email to {Email}: {Errors}", toEmail, errors.ToString());
                }
            }
            else
            {
                _logger.LogError("Failed to send email to {Email}. Status: {Status}, Response: {Response}", 
                    toEmail, response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // Don't throw - log error but continue operation
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://api.aqlaan.com/auth";
        var verificationUrl = $"{baseUrl}/verify-email?token={verificationToken}&email={WebUtility.UrlEncode(toEmail)}";

        var subject = "🔐 Verify Your Email Address";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; }}
        .content h2 {{ color: #667eea; margin-top: 0; }}
        .button {{ display: inline-block; background-color: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background-color: #764ba2; }}
        .token-box {{ background-color: #f8f9fa; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; font-family: monospace; word-break: break-all; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid: #ffc107; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✉️ Email Verification</h1>
        </div>
        <div class=""content"">
            <h2>Welcome to Aqlaan Services!</h2>
            <p>Thank you for signing up. We're excited to have you on board!</p>
            <p>To complete your registration and activate your account, please verify your email address by clicking the button below:</p>
            
            <div style=""text-align: center;"">
                <a href=""{verificationUrl}"" class=""button"">✓ Verify Email Address</a>
            </div>
            
            <div class=""warning"">
                <strong>⏰ Important:</strong> This verification link will expire in <strong>24 hours</strong>.
            </div>
            
            <p>If the button doesn't work, copy and paste this URL into your browser:</p>
            <div class=""token-box"">
                {verificationUrl}
            </div>
            
            <p><strong>Your Verification Code:</strong></p>
            <div class=""token-box"">
                {verificationToken}
            </div>
            
            <div class=""warning"">
                <strong>🔒 Security Note:</strong> If you didn't create an account with us, please ignore this email. Your email address will not be used without verification.
            </div>
        </div>
        <div class=""footer"">
            <p><strong>Aqlaan Services</strong><br>
            Secure Authentication System</p>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2025 Aqlaan. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://api.aqlaan.com/auth";
        var resetUrl = $"{baseUrl}/reset-password?token={resetToken}&email={WebUtility.UrlEncode(toEmail)}";

        var subject = "🔑 Reset Your Password";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; }}
        .content h2 {{ color: #dc3545; margin-top: 0; }}
        .button {{ display: inline-block; background-color: #dc3545; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background-color: #c82333; }}
        .token-box {{ background-color: #f8f9fa; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; font-family: monospace; word-break: break-all; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .danger {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔑 Password Reset</h1>
        </div>
        <div class=""content"">
            <h2>Password Reset Request</h2>
            <p>We received a request to reset your password. If you made this request, click the button below to create a new password:</p>
            
            <div style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </div>
            
            <div class=""warning"">
                <strong>⏰ Important:</strong> This reset link will expire in <strong>1 hour</strong>.
            </div>
            
            <p>If the button doesn't work, copy and paste this URL into your browser:</p>
            <div class=""token-box"">
                {resetUrl}
            </div>
            
            <p><strong>Your Reset Code:</strong></p>
            <div class=""token-box"">
                {resetToken}
            </div>
            
            <div class=""danger"">
                <strong>⚠️ Security Alert:</strong> If you didn't request a password reset, please ignore this email and your password will remain unchanged. Someone may have entered your email address by mistake.
            </div>
            
            <p>For your security:</p>
            <ul>
                <li>Never share your reset link or code with anyone</li>
                <li>Our support team will never ask for your password</li>
                <li>Always verify the URL before entering sensitive information</li>
            </ul>
        </div>
        <div class=""footer"">
            <p><strong>Aqlaan Services</strong><br>
            Secure Authentication System</p>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2025 Aqlaan. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}

// Console-based email service for development/testing
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("EMAIL TO: {Email}\nSUBJECT: {Subject}\nBODY:\n{Body}\n---", toEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task SendVerificationEmailAsync(string toEmail, string verificationToken)
    {
        var message = $"EMAIL VERIFICATION for {toEmail}:\nToken: {verificationToken}\nVerification URL: /auth/verify-email?token={verificationToken}&email={toEmail}";
        _logger.LogInformation(message);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var message = $"PASSWORD RESET for {toEmail}:\nToken: {resetToken}\nReset URL: /auth/reset-password?token={resetToken}&email={toEmail}";
        _logger.LogInformation(message);
        return Task.CompletedTask;
    }
}
