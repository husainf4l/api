using System.ComponentModel.DataAnnotations;

namespace EmailService.DTOs
{
    public class GenerateHtmlRequest
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Heading is required")]
        public string Heading { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; } = string.Empty;

        public string? ButtonText { get; set; }

        [Url(ErrorMessage = "Button URL must be a valid URL")]
        public string? ButtonUrl { get; set; }

        public string? AdditionalInfo { get; set; }

        public string? FooterText { get; set; }
    }
}
