using System;

namespace EmailService.Models
{
    public class EmailLog
    {
        public int Id { get; set; }
        public string To { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string? ApiKeyUsed { get; set; }
    }
}
