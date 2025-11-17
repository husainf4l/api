# AuthService API Guide

## How to Register an Application and Get API Keys

### Base URL
- Production: `https://api.aqlaan.com/auth`
- Local: `http://localhost:5101/auth`

---

## Step 1: Register a User Account

First, you need to register a user account that will manage applications.

**Endpoint:** `POST /api/auth/register`

```bash
curl -X POST https://api.aqlaan.com/auth/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "YourSecure@Password123",
    "applicationCode": "your-app-code"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": "guid-here",
    "email": "admin@example.com"
  }
}
```

---

## Step 2: Login to Get Access Token

**Endpoint:** `POST /api/auth/login`

```bash
curl -X POST https://api.aqlaan.com/auth/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "YourSecure@Password123",
    "applicationCode": "your-app-code"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiresIn": 900,
    "user": {
      "id": "guid",
      "email": "admin@example.com"
    }
  }
}
```

**Save the accessToken for subsequent requests!**

---

## Step 3: Create a New Application

**Endpoint:** `POST /admin/apps`

```bash
curl -X POST https://api.aqlaan.com/auth/admin/apps \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "name": "My Application",
    "code": "my-app",
    "description": "My application description"
  }'
```

**Request Body:**
- `name` (required): Application display name (max 100 chars)
- `code` (required): Unique application code (alphanumeric, underscores, hyphens only)
- `description` (optional): Application description

**Response:**
```json
{
  "success": true,
  "message": "Application created successfully",
  "data": {
    "id": "app-guid-here",
    "name": "My Application",
    "code": "my-app",
    "createdAt": "2025-11-17T...",
    "isActive": true
  }
}
```

---

## Step 4: Create an API Key for Your Application

**Endpoint:** `POST /admin/{appCode}/api-keys`

```bash
curl -X POST https://api.aqlaan.com/auth/admin/my-app/api-keys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "name": "Production API Key",
    "description": "API key for production environment",
    "scopes": ["read", "write", "admin"],
    "environment": "production",
    "expiresInDays": 365,
    "rateLimitPerHour": 1000
  }'
```

**Request Body:**
- `name` (required): API key display name
- `description` (optional): Key description
- `scopes` (required): Array of permission scopes (e.g., ["read", "write", "admin"])
- `environment` (required): "development" or "production"
- `expiresInDays` (optional): Number of days until expiration
- `rateLimitPerHour` (optional): Rate limit per hour

**Response:**
```json
{
  "success": true,
  "message": "API key created successfully",
  "data": {
    "id": "key-guid",
    "name": "Production API Key",
    "apiKey": "ak_live_abc123xyz789...",
    "scopes": ["read", "write", "admin"],
    "expiresAt": "2026-11-17T...",
    "createdAt": "2025-11-17T..."
  }
}
```

⚠️ **IMPORTANT:** Save the `apiKey` value immediately! It will only be shown once.

---

## Step 5: List All Applications

**Endpoint:** `GET /admin/apps`

```bash
curl -X GET https://api.aqlaan.com/auth/admin/apps \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## Step 6: List API Keys for an Application

**Endpoint:** `GET /admin/{appCode}/api-keys`

```bash
curl -X GET https://api.aqlaan.com/auth/admin/my-app/api-keys?page=1&pageSize=50 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**Query Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 50)

---

## Step 7: Validate an API Key

**Endpoint:** `POST /admin/{appCode}/api-keys/validate`

```bash
curl -X POST https://api.aqlaan.com/auth/admin/my-app/api-keys/validate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "apiKey": "ak_live_abc123xyz789...",
    "requestedScope": "read",
    "ipAddress": "192.168.1.1",
    "userAgent": "MyApp/1.0"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "isValid": true,
    "message": "API key is valid",
    "applicationId": "app-guid",
    "userId": "user-guid",
    "scopes": ["read", "write", "admin"]
  }
}
```

---

## Step 8: Revoke an API Key

**Endpoint:** `DELETE /admin/{appCode}/api-keys/{keyId}`

```bash
curl -X DELETE https://api.aqlaan.com/auth/admin/my-app/api-keys/{KEY_ID} \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## Complete Example Script

```bash
#!/bin/bash

BASE_URL="https://api.aqlaan.com/auth"

# Step 1: Register user
echo "1. Registering user..."
REGISTER_RESPONSE=$(curl -s -X POST $BASE_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "SecurePass@123",
    "applicationCode": "initial-app"
  }')

echo $REGISTER_RESPONSE | jq .

# Step 2: Login
echo -e "\n2. Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST $BASE_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "SecurePass@123",
    "applicationCode": "initial-app"
  }')

ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')
echo "Access Token: $ACCESS_TOKEN"

# Step 3: Create application
echo -e "\n3. Creating application..."
APP_RESPONSE=$(curl -s -X POST $BASE_URL/admin/apps \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{
    "name": "My New App",
    "code": "my-new-app",
    "description": "Test application"
  }')

echo $APP_RESPONSE | jq .

# Step 4: Create API key
echo -e "\n4. Creating API key..."
KEY_RESPONSE=$(curl -s -X POST $BASE_URL/admin/my-new-app/api-keys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{
    "name": "Production Key",
    "description": "Main API key",
    "scopes": ["read", "write"],
    "environment": "production",
    "expiresInDays": 365
  }')

echo $KEY_RESPONSE | jq .
API_KEY=$(echo $KEY_RESPONSE | jq -r '.data.apiKey')
echo -e "\n✅ Your API Key: $API_KEY"
echo "⚠️  Save this key! It won't be shown again."
```

---

## Authentication

All admin endpoints require JWT authentication. Include the access token in the Authorization header:

```
Authorization: Bearer YOUR_ACCESS_TOKEN
```

Access tokens expire after 15 minutes. Use the refresh token to get a new access token:

**Endpoint:** `POST /api/auth/refresh`

```bash
curl -X POST https://api.aqlaan.com/auth/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

---

## Error Responses

All endpoints return a consistent error format:

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

Common HTTP status codes:
- `200 OK`: Success
- `201 Created`: Resource created
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing or invalid authentication
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## Best Practices

1. **Secure Storage**: Store API keys securely (environment variables, secrets manager)
2. **Key Rotation**: Rotate keys periodically (use `expiresInDays`)
3. **Scope Management**: Use minimal required scopes
4. **Environment Separation**: Use different keys for development/production
5. **Monitoring**: Track API key usage and set appropriate rate limits
6. **Revocation**: Immediately revoke compromised keys

---

## Support

For issues or questions:
- Documentation: https://api.aqlaan.com/auth/swagger
- Email: support@aqlaan.com
