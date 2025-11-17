using System.Net;
using System.Net.Mail;

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

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var smtpHost = smtpSettings["Host"] ?? "localhost";
            var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];
            var smtpEnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
            var fromEmail = smtpSettings["FromEmail"] ?? "noreply@yourdomain.com";
            var fromName = smtpSettings["FromName"] ?? "Auth Service";

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = !string.IsNullOrEmpty(smtpUsername) ? new NetworkCredential(smtpUsername, smtpPassword) : null,
                EnableSsl = smtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // In production, you might want to queue emails or use a service like SendGrid
            // For now, we'll log the error but not fail the operation
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:8080";
        var verificationUrl = $"{baseUrl}/auth/verify-email?token={verificationToken}&email={WebUtility.UrlEncode(toEmail)}";

        var subject = "Verify Your Email Address";
        var body = $@"
<html>
<body>
    <h2>Welcome to Auth Service!</h2>
    <p>Please verify your email address by clicking the link below:</p>
    <p><a href=""{verificationUrl}"" style=""background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Verify Email Address</a></p>
    <p>If the button doesn't work, copy and paste this URL into your browser:</p>
    <p>{verificationUrl}</p>
    <p>This link will expire in 24 hours.</p>
    <p>If you didn't create an account, please ignore this email.</p>
    <br>
    <p>Best regards,<br>The Auth Service Team</p>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:8080";
        var resetUrl = $"{baseUrl}/auth/reset-password?token={resetToken}&email={WebUtility.UrlEncode(toEmail)}";

        var subject = "Reset Your Password";
        var body = $@"
<html>
<body>
    <h2>Password Reset Request</h2>
    <p>You requested to reset your password. Click the link below to create a new password:</p>
    <p><a href=""{resetUrl}"" style=""background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
    <p>If the button doesn't work, copy and paste this URL into your browser:</p>
    <p>{resetUrl}</p>
    <p>This link will expire in 1 hour.</p>
    <p>If you didn't request a password reset, please ignore this email.</p>
    <br>
    <p>Best regards,<br>The Auth Service Team</p>
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
