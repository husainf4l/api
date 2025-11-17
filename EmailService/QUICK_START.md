# EmailService - Quick Integration Guide

## 🚀 Quick Start

Your EmailService is running and ready to accept email requests from any application!

**Service URL**: `http://localhost:5189`

---

## 📨 How to Send an Email

### Using cURL (Command Line)

```bash
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{
    "to": "recipient@example.com",
    "subject": "Your Subject",
    "body": "Your message here",
    "isHtml": false
  }'
```

### Using Postman
1. Create a new POST request
2. URL: `http://localhost:5189/api/email/send`
3. Headers: `Content-Type: application/json`
4. Body (raw JSON):
```json
{
  "to": "recipient@example.com",
  "subject": "Your Subject",
  "body": "Your message here",
  "isHtml": false
}
```

---

## 🔧 Integration Examples

### JavaScript / Node.js
```javascript
const response = await fetch('http://localhost:5189/api/email/send', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    to: 'recipient@example.com',
    subject: 'Hello',
    body: 'Your message',
    isHtml: false
  })
});
const result = await response.json();
console.log(result);
```

### Python
```python
import requests

response = requests.post('http://localhost:5189/api/email/send', json={
    'to': 'recipient@example.com',
    'subject': 'Hello',
    'body': 'Your message',
    'isHtml': False
})
print(response.json())
```

### C# / .NET
```csharp
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient();
var json = JsonSerializer.Serialize(new {
    to = "recipient@example.com",
    subject = "Hello",
    body = "Your message",
    isHtml = false
});
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("http://localhost:5189/api/email/send", content);
```

### Java
```java
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;

CloseableHttpClient client = HttpClients.createDefault();
HttpPost httpPost = new HttpPost("http://localhost:5189/api/email/send");
String json = "{\"to\":\"recipient@example.com\",\"subject\":\"Hello\",\"body\":\"Message\",\"isHtml\":false}";
httpPost.setEntity(new StringEntity(json));
httpPost.setHeader("Content-Type", "application/json");
client.execute(httpPost);
```

### PHP
```php
$data = [
    'to' => 'recipient@example.com',
    'subject' => 'Hello',
    'body' => 'Your message',
    'isHtml' => false
];

$ch = curl_init('http://localhost:5189/api/email/send');
curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
$result = curl_exec($ch);
```

---

## ✅ Current Status

- **Service**: ✅ Running on `http://localhost:5189`
- **AWS Region**: me-central-1 (UAE)
- **Sender Email**: admin@rolevate.com
- **Status**: Ready for email delivery

---

## 📋 Request Parameters

| Field | Type | Required | Example |
|-------|------|----------|---------|
| `to` | string | Yes | "recipient@example.com" |
| `cc` | string | No | "cc@example.com" |
| `bcc` | string | No | "bcc@example.com" |
| `subject` | string | Yes | "Hello" |
| `body` | string | Yes | "Your message" |
| `isHtml` | boolean | No | true/false |

---

## 🎯 Response Examples

### Success (200 OK)
```json
{
  "success": true,
  "message": "Email sent successfully",
  "messageId": "011d019a91fc0b6b-368d8322-5038-4004-9d57-998ba9c9533c-000000"
}
```

### Error (400/500)
```json
{
  "success": false,
  "message": "Failed to send email: Email address is not verified",
  "messageId": null
}
```

---

## ⚠️ Important: Email Verification

**Current Limitation**: Due to AWS SES Sandbox mode, emails can only be sent to these verified addresses:
- `husain.f4l@gmail.com`
- `admin@rolevate.com`
- `info@aqlaan.com`
- Any email under `@aqlaan.com` or `@rolevate.com` domains

**To send to any email address**, request AWS SES Production Access via AWS Support Console.

---

## 🧪 Test It Now

```bash
# Test endpoint
curl http://localhost:5189/api/email/health

# Send a test email
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{
    "to": "husain.f4l@gmail.com",
    "subject": "Test",
    "body": "Hello World",
    "isHtml": false
  }'
```

---

## 📚 Full Documentation

See `EMAIL_API_DOCUMENTATION.md` for complete API reference and more examples.
