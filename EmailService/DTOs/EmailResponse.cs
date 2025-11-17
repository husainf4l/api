namespace EmailService.DTOs
{
    public class EmailResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? MessageId { get; set; }
    }
}
