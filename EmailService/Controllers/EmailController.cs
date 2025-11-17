using EmailService.DTOs;
using EmailService.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EmailService.Controllers
{
    /// <summary>
    /// Email API controller for sending emails via AWS SES
    /// </summary>
    [ApiController]
    [Route("")]
    [Produces("application/json")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send an email
        /// </summary>
        /// <param name="request">Email details</param>
        /// <returns>Email send result</returns>
        /// <response code="200">Email sent successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Server error</response>
        [HttpPost("send")]
        [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmailResponse>> SendEmail([FromBody] SendEmailRequest request)
        {
            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(new EmailResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            _logger.LogInformation("Sending email to {To} with subject: {Subject}", request.To, request.Subject);

            var response = await _emailService.SendEmailAsync(request);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }

        /// <summary>
        /// Generate HTML email from template
        /// </summary>
        /// <param name="request">Template data</param>
        /// <returns>Generated HTML email</returns>
        /// <response code="200">HTML generated successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("generate-html")]
        [ProducesResponseType(typeof(GenerateHtmlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GenerateHtml([FromBody] GenerateHtmlRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data" });
            }

            var html = GenerateH245Template(request);
            
            return Ok(new GenerateHtmlResponse
            {
                Success = true,
                Html = html,
                Message = "HTML generated successfully"
            });
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service status</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new 
            { 
                status = "healthy",
                service = "EmailService",
                timestamp = DateTime.UtcNow
            });
        }

        private string GenerateH245Template(GenerateHtmlRequest request)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{request.Title}</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f4f4f4;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content h2 {{
            color: #333333;
            font-size: 24px;
            margin-top: 0;
            margin-bottom: 20px;
        }}
        .content p {{
            color: #666666;
            font-size: 16px;
            line-height: 1.6;
            margin: 0 0 15px 0;
        }}
        .button {{
            display: inline-block;
            padding: 14px 30px;
            margin: 20px 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: 600;
            font-size: 16px;
        }}
        .button:hover {{
            opacity: 0.9;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            color: #999999;
            font-size: 14px;
            margin: 5px 0;
        }}
        .divider {{
            height: 1px;
            background-color: #e9ecef;
            margin: 30px 0;
        }}
        @media only screen and (max-width: 600px) {{
            .email-container {{
                width: 100% !important;
            }}
            .header, .content, .footer {{
                padding: 20px !important;
            }}
            .header h1 {{
                font-size: 24px !important;
            }}
            .content h2 {{
                font-size: 20px !important;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>{request.Title}</h1>
        </div>
        <div class=""content"">
            <h2>{request.Heading}</h2>
            <p>{request.Message}</p>
            {(string.IsNullOrEmpty(request.ButtonText) || string.IsNullOrEmpty(request.ButtonUrl) ? "" : 
            $@"<div style=""text-align: center;"">
                <a href=""{request.ButtonUrl}"" class=""button"">{request.ButtonText}</a>
            </div>")}
            {(string.IsNullOrEmpty(request.AdditionalInfo) ? "" : 
            $@"<div class=""divider""></div>
            <p style=""color: #999999; font-size: 14px;"">{request.AdditionalInfo}</p>")}
        </div>
        <div class=""footer"">
            <p>{request.FooterText ?? "Thank you for using our service"}</p>
            <p style=""font-size: 12px; color: #bbbbbb;"">© {DateTime.UtcNow.Year} All rights reserved</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
