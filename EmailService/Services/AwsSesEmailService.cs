using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using EmailService.DTOs;
using EmailService.Data;
using EmailService.Models;

namespace EmailService.Services
{
    public interface IEmailService
    {
        Task<EmailResponse> SendEmailAsync(DTOs.SendEmailRequest request);
    }

    public class AwsSesEmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly ILogger<AwsSesEmailService> _logger;
        private readonly EmailDbContext _dbContext;
        private readonly string _fromEmail;

        public AwsSesEmailService(
            IAmazonSimpleEmailService sesClient, 
            ILogger<AwsSesEmailService> logger, 
            EmailDbContext dbContext,
            IConfiguration configuration)
        {
            _sesClient = sesClient;
            _logger = logger;
            _dbContext = dbContext;
            _fromEmail = configuration["EmailSettings:FromEmail"] ?? "noreply@example.com";
        }

        public async Task<EmailResponse> SendEmailAsync(DTOs.SendEmailRequest request)
        {
            var emailLog = new EmailLog
            {
                To = request.To,
                Cc = request.Cc,
                Bcc = request.Bcc,
                Subject = request.Subject,
                Body = request.Body,
                IsHtml = request.IsHtml,
                SentAt = DateTime.UtcNow
            };

            try
            {
                var sendRequest = new Amazon.SimpleEmail.Model.SendEmailRequest
                {
                    Source = _fromEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { request.To }
                    },
                    Message = new Message
                    {
                        Subject = new Content(request.Subject),
                        Body = new Body
                        {
                            Html = request.IsHtml ? new Content(request.Body) : null,
                            Text = !request.IsHtml ? new Content(request.Body) : null
                        }
                    }
                };

                // Add CC if provided
                if (!string.IsNullOrEmpty(request.Cc))
                {
                    sendRequest.Destination.CcAddresses.Add(request.Cc);
                }

                // Add BCC if provided
                if (!string.IsNullOrEmpty(request.Bcc))
                {
                    sendRequest.Destination.BccAddresses.Add(request.Bcc);
                }

                var response = await _sesClient.SendEmailAsync(sendRequest);

                _logger.LogInformation($"Email sent successfully to {request.To}. MessageId: {response.MessageId}");

                // Update log with success
                emailLog.Success = true;
                emailLog.MessageId = response.MessageId;

                // Save to database
                _dbContext.EmailLogs.Add(emailLog);
                await _dbContext.SaveChangesAsync();

                return new EmailResponse
                {
                    Success = true,
                    Message = "Email sent successfully",
                    MessageId = response.MessageId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");

                // Update log with failure
                emailLog.Success = false;
                emailLog.ErrorMessage = ex.Message;

                // Save to database
                _dbContext.EmailLogs.Add(emailLog);
                await _dbContext.SaveChangesAsync();

                return new EmailResponse
                {
                    Success = false,
                    Message = $"Failed to send email: {ex.Message}"
                };
            }
        }
    }
}
