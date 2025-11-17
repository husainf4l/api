# 🔐 EmailService - Secure API with API Key Authentication

## ✅ Security Implementation Complete

The EmailService now requires API key authentication for all email sending requests!

---

## 🔑 API Key Configuration

**Development API Key**: `dev-email-service-key-2024`

**Location**: 
- `appsettings.Development.json`
- `.env` file

**⚠️ Important**: Change the API key in production!

---

## 📨 How to Send Emails with API Key

### Required Header
All requests to `/api/email/send` must include:

```
X-API-Key: dev-email-service-key-2024
```

---

## 🧪 Usage Examples

### Example 1: cURL with API Key

```bash
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{
    "to": "recipient@example.com",
    "subject": "Hello World",
    "body": "Your message here",
    "isHtml": false
  }'
```

### Example 2: JavaScript / Node.js

```javascript
const response = await fetch('http://localhost:5189/api/email/send', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'dev-email-service-key-2024'
  },
  body: JSON.stringify({
    to: 'recipient@example.com',
    subject: 'Hello',
    body: 'Your message',
    isHtml: false
  })
});
```

### Example 3: Python

```python
import requests

headers = {
    'Content-Type': 'application/json',
    'X-API-Key': 'dev-email-service-key-2024'
}

data = {
    'to': 'recipient@example.com',
    'subject': 'Hello',
    'body': 'Your message',
    'isHtml': False
}

response = requests.post(
    'http://localhost:5189/api/email/send',
    headers=headers,
    json=data
)
print(response.json())
```

### Example 4: C# / .NET

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "dev-email-service-key-2024");

var json = JsonSerializer.Serialize(new {
    to = "recipient@example.com",
    subject = "Hello",
    body = "Your message",
    isHtml = false
});

var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("http://localhost:5189/api/email/send", content);
```

### Example 5: PHP

```php
$ch = curl_init('http://localhost:5189/api/email/send');

$data = [
    'to' => 'recipient@example.com',
    'subject' => 'Hello',
    'body' => 'Your message',
    'isHtml' => false
];

$headers = [
    'Content-Type: application/json',
    'X-API-Key: dev-email-service-key-2024'
];

curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

$response = curl_exec($ch);
curl_close($ch);
```

---

## 🛡️ Security Responses

### ✅ Success (with valid API key)
```json
{
  "success": true,
  "message": "Email sent successfully",
  "messageId": "011d019a9219b66a-9730c1a3-7731-4ccc-a5c7-26909c94330d-000000"
}
```

### ❌ Missing API Key (401 Unauthorized)
```json
{
  "error": "API Key is missing"
}
```

### ❌ Invalid API Key (401 Unauthorized)
```json
{
  "error": "Invalid API Key"
}
```

---

## 🚫 Endpoints WITHOUT API Key Requirement

The following endpoints are public and don't require API key:

- `GET /api/email/health` - Health check endpoint

```bash
curl http://localhost:5189/api/email/health
# Response: {"status":"Email service is healthy"}
```

---

## 🔐 Changing the API Key

### For Development
Edit `appsettings.Development.json`:
```json
{
  "ApiSettings": {
    "ApiKey": "your-new-api-key-here"
  }
}
```

### For Production
Edit `appsettings.json`:
```json
{
  "ApiSettings": {
    "ApiKey": "your-secure-production-key"
  }
}
```

### Using Environment Variables
Set the environment variable:
```bash
export ApiSettings__ApiKey="your-api-key"
```

---

## 💡 Best Practices

1. **Use Strong API Keys**: Generate random, complex API keys for production
2. **Keep Keys Secret**: Never commit API keys to version control
3. **Rotate Keys Regularly**: Change API keys periodically
4. **Use Environment Variables**: Store production keys in environment variables or secure vaults
5. **Monitor Usage**: Log all API key usage for security auditing

---

## 🔒 Example: Generate Secure API Key

```bash
# Generate a secure random API key (Linux/Mac)
openssl rand -base64 32

# Or using Python
python3 -c "import secrets; print(secrets.token_urlsafe(32))"
```

---

## ✅ Current Configuration

- **Service URL**: `http://localhost:5189`
- **API Key Header**: `X-API-Key`
- **Development Key**: `dev-email-service-key-2024`
- **Protected Endpoints**: `/api/email/send`
- **Public Endpoints**: `/api/email/health`

---

## 📋 Quick Test

```bash
# Test without API key (should fail)
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -d '{"to":"test@example.com","subject":"Test","body":"Test","isHtml":false}'

# Test with valid API key (should succeed)
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{"to":"husain.f4l@gmail.com","subject":"Secure Test","body":"This works!","isHtml":false}'
```

---

**Your EmailService is now secured with API key authentication!** 🔐
