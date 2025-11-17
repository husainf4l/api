namespace WhatsApp.Models;

public class TemplateResponse
{
    public List<TemplateData>? Data { get; set; }
    public object? Paging { get; set; }
}

public class TemplateData
{
    public string? Name { get; set; }
    public string? Language { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Id { get; set; }
}

public class SendMessageResponse
{
    public string? MessagingProduct { get; set; }
    public List<Contact>? Contacts { get; set; }
    public List<Message>? Messages { get; set; }
}

public class Contact
{
    public string? Input { get; set; }
    public string? WaId { get; set; }
}

public class Message
{
    public string? Id { get; set; }
}

public class WhatsAppError
{
    public ErrorDetails? Error { get; set; }
}

public class ErrorDetails
{
    public string? Message { get; set; }
    public string? Type { get; set; }
    public int Code { get; set; }
    public string? FbtraceId { get; set; }
}

public class WebhookPayload
{
    public string? Object { get; set; }
    public List<WebhookEntry>? Entry { get; set; }
}

public class WebhookEntry
{
    public string? Id { get; set; }
    public List<WebhookChange>? Changes { get; set; }
    public List<WebhookMessaging>? Messaging { get; set; }
}

public class WebhookChange
{
    public string? Field { get; set; }
    public WebhookValue? Value { get; set; }
}

public class WebhookValue
{
    public string? MessagingProduct { get; set; }
    public object? Metadata { get; set; }
    public List<WebhookMessage>? Messages { get; set; }
    public List<WebhookStatus>? Statuses { get; set; }
}

public class WebhookMessage
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? Timestamp { get; set; }
    public string? Type { get; set; }
    public TextMessage? Text { get; set; }
    public ImageMessage? Image { get; set; }
    public DocumentMessage? Document { get; set; }
}

public class TextMessage
{
    public string? Body { get; set; }
}

public class ImageMessage
{
    public string? Id { get; set; }
    public string? MimeType { get; set; }
    public string? Sha256 { get; set; }
}

public class DocumentMessage
{
    public string? Id { get; set; }
    public string? MimeType { get; set; }
    public string? Sha256 { get; set; }
    public string? FileName { get; set; }
}

public class WebhookStatus
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Timestamp { get; set; }
    public string? RecipientId { get; set; }
}

public class WebhookMessaging
{
    public WebhookMessage? Message { get; set; }
    public List<WebhookContact>? Contacts { get; set; }
    public string? Timestamp { get; set; }
}

public class WebhookContact
{
    public string? WaId { get; set; }
    public WebhookProfile? Profile { get; set; }
}

public class WebhookProfile
{
    public string? Name { get; set; }
}
