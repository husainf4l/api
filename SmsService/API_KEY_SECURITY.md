# SMS Service API Key Authentication

## 🔐 Security Overview

The SMS Service uses **API Key authentication** to secure all endpoints (except health check).

## API Key Details

### Production API Key
```
sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=
```

**⚠️ IMPORTANT:** Keep this key secure and never commit it to version control!

## How to Use

### Include in Request Header

All API requests must include the API key in the header:

```
X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=
```

### Example Requests

#### Using curl
```bash
curl -X POST http://localhost:5103/api/sms/send/otp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=" \
  -d '{
    "to": "962771122003",
    "message": "Your OTP code is: 123456"
  }'
```

#### Using JavaScript/TypeScript
```javascript
const response = await fetch('http://localhost:5103/api/sms/send/general', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I='
  },
  body: JSON.stringify({
    to: '962771122003',
    message: 'Hello from SMS Service!'
  })
});
```

#### Using C# / .NET
```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-API-Key", "sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=");

var content = new StringContent(
    JsonSerializer.Serialize(new { 
        to = "962771122003", 
        message = "Hello!" 
    }),
    Encoding.UTF8,
    "application/json"
);

var response = await httpClient.PostAsync(
    "http://localhost:5103/api/sms/send/general",
    content
);
```

## Protected Endpoints

The following endpoints require API key authentication:

- ✅ `GET /api/sms/balance` - Get SMS balance
- ✅ `POST /api/sms/send/otp` - Send OTP SMS
- ✅ `POST /api/sms/send/general` - Send general SMS
- ✅ `POST /api/sms/send/bulk` - Send bulk SMS

## Public Endpoints

These endpoints do NOT require authentication:

- 🌐 `GET /api/sms/health` - Health check

## Error Responses

### Missing API Key
```json
{
  "error": "API Key is missing"
}
```
**Status Code:** 401 Unauthorized

### Invalid API Key
```json
{
  "error": "Invalid API Key"
}
```
**Status Code:** 401 Unauthorized

## Security Best Practices

1. **Never commit API keys to Git**
   - Use `.env` files (already in `.gitignore`)
   - Use environment variables in production

2. **Rotate keys regularly**
   - Generate new key: `openssl rand -base64 32`
   - Update in `.env` and `appsettings.json`

3. **Use HTTPS in production**
   - Prevents API key interception
   - Configure SSL/TLS certificates

4. **Monitor API usage**
   - Check logs for unauthorized attempts
   - Track API key usage patterns

5. **Implement rate limiting** (Future enhancement)
   - Prevent abuse
   - Limit requests per minute/hour

## Generating New API Keys

To generate a new secure API key:

```bash
openssl rand -base64 32
```

Then update:
1. `.env` file: `API_KEY=your-new-key`
2. `appsettings.json`: Update `ApiSettings.ApiKey`

## Configuration Files

### .env
```env
API_KEY=sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=
```

### appsettings.json
```json
{
  "ApiSettings": {
    "ApiKey": "sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I="
  }
}
```

## Middleware Implementation

The API key validation is handled by `ApiKeyAuthMiddleware.cs`:

- Checks for `X-API-Key` header
- Validates against configured key
- Returns 401 if missing or invalid
- Allows health check without authentication

## Testing Authentication

### Test with valid key (✅ Success)
```bash
curl -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=" \
  http://localhost:5103/api/sms/balance
```

### Test without key (❌ Unauthorized)
```bash
curl http://localhost:5103/api/sms/balance
```

### Test with invalid key (❌ Unauthorized)
```bash
curl -H "X-API-Key: wrong-key" \
  http://localhost:5103/api/sms/balance
```

---

**Last Updated:** November 17, 2025  
**Key Generated:** Using `openssl rand -base64 32`
