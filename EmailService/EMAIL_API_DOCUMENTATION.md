# EmailService API Documentation

## Overview
The EmailService provides a REST API for sending emails via AWS SES in the UAE region (me-central-1).

**Base URL**: `http://localhost:5189`

---

## Endpoints

### 1. Send Email

**Endpoint**: `POST /api/email/send`

**Content-Type**: `application/json`

#### Request Body

```json
{
  "to": "recipient@example.com",
  "cc": "optional@example.com",
  "bcc": "optional@example.com",
  "subject": "Email Subject",
  "body": "Email body or HTML content",
  "isHtml": false
}
```

#### Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `to` | string | Yes | Recipient email address |
| `cc` | string | No | Carbon copy email address |
| `bcc` | string | No | Blind carbon copy email address |
| `subject` | string | Yes | Email subject line |
| `body` | string | Yes | Email body (text or HTML) |
| `isHtml` | boolean | No | Set to `true` for HTML content (default: `false`) |

#### Success Response

**Status Code**: `200 OK`

```json
{
  "success": true,
  "message": "Email sent successfully",
  "messageId": "011d019a91fc0b6b-368d8322-5038-4004-9d57-998ba9c9533c-000000"
}
```

#### Error Response

**Status Code**: `400` or `500`

```json
{
  "success": false,
  "message": "Failed to send email: Email address is not verified",
  "messageId": null
}
```

---

## Usage Examples

### Example 1: Send Plain Text Email using cURL

```bash
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{
    "to": "recipient@example.com",
    "subject": "Hello World",
    "body": "This is a test email",
    "isHtml": false
  }'
```

### Example 2: Send HTML Email using cURL

```bash
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{
    "to": "recipient@example.com",
    "subject": "Welcome",
    "body": "<h1>Welcome!</h1><p>This is an HTML email</p>",
    "isHtml": true
  }'
```

### Example 3: Send Email with CC and BCC using cURL

```bash
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{
    "to": "recipient@example.com",
    "cc": "cc@example.com",
    "bcc": "bcc@example.com",
    "subject": "Important Update",
    "body": "Email body content",
    "isHtml": false
  }'
```

### Example 4: C# / .NET Application

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
var url = "http://localhost:5189/api/email/send";

var emailData = new
{
    to = "recipient@example.com",
    subject = "Test Email",
    body = "Hello from C# application",
    isHtml = false
};

var json = JsonSerializer.Serialize(emailData);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync(url, content);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

### Example 5: Python Application

```python
import requests
import json

url = "http://localhost:5189/api/email/send"

payload = {
    "to": "recipient@example.com",
    "subject": "Test Email from Python",
    "body": "Hello from Python application",
    "isHtml": False
}

headers = {
    "Content-Type": "application/json"
}

response = requests.post(url, json=payload, headers=headers)
print(response.json())
```

### Example 6: JavaScript / Node.js

```javascript
const axios = require('axios');

const emailData = {
    to: 'recipient@example.com',
    subject: 'Test Email from Node.js',
    body: 'Hello from Node.js application',
    isHtml: false
};

axios.post('http://localhost:5189/api/email/send', emailData)
    .then(response => console.log(response.data))
    .catch(error => console.error(error));
```

### Example 7: PHP Application

```php
<?php
$url = 'http://localhost:5189/api/email/send';

$data = [
    'to' => 'recipient@example.com',
    'subject' => 'Test Email from PHP',
    'body' => 'Hello from PHP application',
    'isHtml' => false
];

$ch = curl_init($url);
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));

$response = curl_exec($ch);
curl_close($ch);

echo $response;
?>
```

---

## Health Check

**Endpoint**: `GET /api/email/health`

**Response**:
```json
{
  "status": "Email service is healthy"
}
```

---

## Configuration

### AWS Credentials
The service uses AWS credentials from:
1. Environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
2. Or from `.env` file in the EmailService directory

### Current Configuration
- **Sender Email**: `admin@rolevate.com`
- **AWS Region**: `me-central-1` (UAE)
- **Service URL**: `http://localhost:5189`

---

## Important Notes

### Verified Email Addresses (Current)
Due to AWS SES Sandbox limitations, emails can only be sent to these verified addresses:
- `husain.f4l@gmail.com`
- `admin@rolevate.com`
- `info@aqlaan.com`
- `test@aqlaan.com`
- `test@rolevate.com`

### To Send to Any Email Address
Request **AWS SES Production Access** - Submit a support ticket to AWS with:
- Service: AWS Support
- Category: Service Limit Increase
- Limit Type: SES Sending Limits
- Region: me-central-1
- Request: Production Access removal from Sandbox

---

## Error Codes

| Error | Cause | Solution |
|-------|-------|----------|
| "Email address is not verified" | Recipient email not verified in SES | Verify email or request production access |
| "Failed to send email" | AWS SES error | Check AWS credentials and region |
| 400 Bad Request | Missing required fields | Ensure `to`, `subject`, `body` are provided |

---

## Rate Limits (Current - Sandbox)
- **Daily Limit**: 200 emails per 24 hours
- **Per-Second Rate**: 1 email per second

---

## Support

For issues or questions, check:
1. Service logs at `/tmp/email-service.log`
2. AWS SES verification status
3. Network connectivity to localhost:5189
