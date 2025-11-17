# SMS Message Tracking

## Overview
All SMS messages sent through the service are automatically saved to a PostgreSQL database with complete tracking information including sender details and message metadata.

## Database Schema

```sql
CREATE TABLE sms_messages (
    id SERIAL PRIMARY KEY,
    recipient VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    sender_id VARCHAR(50) NOT NULL,
    message_id VARCHAR(100),          -- JOSMS MsgID
    status VARCHAR(20) NOT NULL,      -- 'sent' or 'failed'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    response_data TEXT,               -- Raw JOSMS response
    ip_address VARCHAR(45),           -- Sender's IP
    user_agent TEXT,                  -- HTTP User-Agent
    api_key_used VARCHAR(100)         -- Masked API key (first 8 chars)
);

CREATE INDEX idx_sms_recipient ON sms_messages(recipient);
CREATE INDEX idx_sms_created_at ON sms_messages(created_at);
CREATE INDEX idx_sms_status ON sms_messages(status);
```

## API Endpoints

### 1. Get All Recent Messages

**Endpoint:** `GET /api/sms/history?limit=100`

**Authentication:** Required (X-API-Key header)

**Query Parameters:**
- `limit` (optional): Number of messages to retrieve (default: 100)

**Response:**
```json
{
  "success": true,
  "count": 3,
  "messages": [
    {
      "id": 1,
      "recipient": "962771122003",
      "message": "Test message",
      "senderId": "MargoGroup",
      "messageId": "59798793",
      "status": "sent",
      "createdAt": "2025-11-17T16:47:36.803388Z",
      "responseData": "MsgID = 59798793",
      "ipAddress": "192.168.1.100",
      "userAgent": "MyApp/1.0",
      "apiKeyUsed": "sms-prod..."
    }
  ]
}
```

### 2. Get Messages by Recipient

**Endpoint:** `GET /api/sms/history/{recipient}`

**Authentication:** Required (X-API-Key header)

**Path Parameters:**
- `recipient`: Phone number (e.g., 962771122003)

**Response:**
```json
{
  "success": true,
  "recipient": "962771122003",
  "count": 2,
  "messages": [...]
}
```

## Tracked Information

### Message Details
- **ID**: Database record ID
- **Recipient**: Normalized phone number (962XXXXXXXXX)
- **Message**: SMS content
- **Sender ID**: Used sender (e.g., MargoGroup)
- **Message ID**: JOSMS MsgID (null for bulk messages)
- **Status**: `sent` or `failed`
- **Created At**: UTC timestamp

### Sender Information
- **IP Address**: IPv4/IPv6 address of the request sender
- **User Agent**: HTTP User-Agent header from the request
- **API Key Used**: First 8 characters of the API key for tracking (e.g., "sms-prod...")

### Response Data
- **Response Data**: Raw response from JOSMS gateway (for debugging)

## Use Cases

### 1. Audit Trail
Track who sent which messages and when:
```sql
SELECT 
    recipient, 
    message, 
    created_at, 
    ip_address, 
    user_agent, 
    api_key_used
FROM sms_messages
WHERE created_at > NOW() - INTERVAL '24 hours'
ORDER BY created_at DESC;
```

### 2. Monitor API Key Usage
Track usage per API key:
```sql
SELECT 
    api_key_used, 
    COUNT(*) as message_count,
    COUNT(DISTINCT recipient) as unique_recipients
FROM sms_messages
GROUP BY api_key_used;
```

### 3. Delivery Status Reports
Check delivery success rate:
```sql
SELECT 
    status,
    COUNT(*) as count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER(), 2) as percentage
FROM sms_messages
GROUP BY status;
```

### 4. Recipient Message History
View all messages sent to a specific number:
```sql
SELECT 
    message, 
    created_at, 
    status, 
    message_id
FROM sms_messages
WHERE recipient = '962771122003'
ORDER BY created_at DESC;
```

## Security Considerations

### API Key Masking
- Only the first 8 characters of the API key are stored
- Full API keys are never saved to the database
- Example: `sms-prod-o02az0sVxwe2...` → `sms-prod...`

### IP Address Tracking
- IPv4 and IPv6 addresses are supported
- Useful for identifying suspicious activity
- Can be used for rate limiting

### Access Control
- History endpoints require authentication
- Only authorized users can view message history
- Implement additional role-based access if needed

## Database Configuration

**Connection String** (in appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=149.200.251.12;Port=5432;Database=aqlaansms;Username=husain;Password=tt55oo77"
  }
}
```

## Example Usage

### Send and Track a Message
```bash
# Send SMS
curl -X POST http://localhost:5103/api/sms/send/general \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=" \
  -H "User-Agent: MyApp/1.0" \
  -d '{
    "to": "962771122003",
    "message": "Your order has been shipped"
  }'

# View history
curl -X GET "http://localhost:5103/api/sms/history?limit=10" \
  -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I="

# View recipient-specific history
curl -X GET "http://localhost:5103/api/sms/history/962771122003" \
  -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I="
```

## Implementation Details

### Automatic Tracking
All send methods automatically save messages:
- `SendOtpAsync()`
- `SendGeneralAsync()`
- `SendBulkAsync()` (saves individual records for each recipient)

### Context Extraction
The service extracts tracking information from HttpContext:
```csharp
private (string? ipAddress, string? userAgent, string? apiKey) ExtractContextInfo(HttpContext? context)
{
    if (context == null) return (null, null, null);
    
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers["User-Agent"].ToString();
    var apiKey = context.Request.Headers["X-API-Key"].ToString();
    
    // Mask API key for security
    if (!string.IsNullOrEmpty(apiKey) && apiKey.Length > 8)
    {
        apiKey = apiKey.Substring(0, 8) + "...";
    }
    
    return (ipAddress, userAgent, apiKey);
}
```

### Repository Pattern
`SmsMessageRepository` handles all database operations:
- `SaveMessageAsync()` - Insert new message records
- `GetRecentMessagesAsync()` - Retrieve recent messages with limit
- `GetMessagesByRecipientAsync()` - Get messages for specific recipient

## Maintenance

### Regular Cleanup
Consider implementing periodic cleanup for old messages:
```sql
-- Delete messages older than 90 days
DELETE FROM sms_messages
WHERE created_at < NOW() - INTERVAL '90 days';
```

### Backup Strategy
- Regular database backups recommended
- Message data contains PII (phone numbers)
- Follow data retention policies

### Monitoring
Monitor database growth:
```sql
SELECT 
    COUNT(*) as total_messages,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '24 hours') as last_24h,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '7 days') as last_7d
FROM sms_messages;
```

---

**Last Updated:** November 17, 2025
