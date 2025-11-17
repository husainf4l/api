using System.ComponentModel.DataAnnotations;

namespace EmailService.DTOs
{
    public class SendEmailRequest
    {
        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string To { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid CC email address")]
        public string? Cc { get; set; }

        [EmailAddress(ErrorMessage = "Invalid BCC email address")]
        public string? Bcc { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Body is required")]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = false;
    }
}
