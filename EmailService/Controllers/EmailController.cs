using EmailService.DTOs;
using EmailService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<EmailResponse>> SendEmail([FromBody] SendEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
            {
                return BadRequest(new EmailResponse
                {
                    Success = false,
                    Message = "Email address, subject, and body are required"
                });
            }

            var response = await _emailService.SendEmailAsync(request);
            return response.Success ? Ok(response) : StatusCode(500, response);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "Email service is healthy" });
        }
    }
}
