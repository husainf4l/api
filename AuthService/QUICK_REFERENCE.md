# Quick Reference - AuthService API

## Base URL
```
https://your-domain.com/api/auth
```

## Endpoints

### 1. Register New User
```http
POST /register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (200 OK):**
```json
{
  "tokens": {
    "accessToken": "eyJhbGciOiJIUzI1Ni...",
    "refreshToken": "base64-encoded-string",
    "expiresAt": "2025-11-17T12:15:00Z",
    "tokenType": "Bearer"
  },
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": false,
    "createdAt": "2025-11-17T11:00:00Z"
  }
}
```

### 2. Login
```http
POST /login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "deviceInfo": "Chrome/Windows 11"
}
```

**Response:** Same as Register

### 3. Refresh Token
```http
POST /refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1Ni...",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2025-11-17T12:15:00Z",
  "tokenType": "Bearer"
}
```

### 4. Logout (Revoke Token)
```http
POST /revoke
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

**Response (200 OK):**
```json
{
  "message": "Token revoked successfully"
}
```

### 5. Validate Token
```http
POST /validate
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1Ni..."
}
```

**Response (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "isEmailVerified": false,
  "createdAt": "2025-11-17T11:00:00Z"
}
```

### 6. Get Current User (Protected)
```http
GET /me
Authorization: Bearer eyJhbGciOiJIUzI1Ni...
```

**Response:** Same as Validate

### 7. Health Check
```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-17T11:00:00Z"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "message": "User with this email already exists"
}
```

### 401 Unauthorized
```json
{
  "message": "Invalid credentials"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred during registration"
}
```

## Using Tokens

### In JavaScript/TypeScript
```javascript
// Store tokens securely
localStorage.setItem('accessToken', tokens.accessToken);
localStorage.setItem('refreshToken', tokens.refreshToken);

// Add to requests
const response = await fetch('https://your-api/endpoint', {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
  }
});

// Refresh when expired
async function refreshAccessToken() {
  const response = await fetch('https://auth-service/api/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      refreshToken: localStorage.getItem('refreshToken')
    })
  });
  const data = await response.json();
  localStorage.setItem('accessToken', data.accessToken);
  localStorage.setItem('refreshToken', data.refreshToken);
  return data.accessToken;
}
```

### In C#
```csharp
// Add token to HttpClient
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

var response = await client.GetAsync("https://your-api/endpoint");
```

### In Python
```python
import requests

headers = {
    'Authorization': f'Bearer {access_token}'
}

response = requests.get('https://your-api/endpoint', headers=headers)
```

## Token Lifetimes

- **Access Token**: 15 minutes (configurable)
- **Refresh Token**: 7 days (configurable)

## Best Practices

1. **Store tokens securely** (httpOnly cookies or secure storage)
2. **Never log tokens** in production
3. **Implement automatic token refresh** before expiry
4. **Handle 401 errors** by refreshing token
5. **Revoke tokens on logout**
6. **Use HTTPS only** in production
7. **Implement rate limiting** on login endpoints
8. **Monitor failed login attempts**

## Integration Example (Complete Flow)

```javascript
class AuthClient {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
  }

  async register(email, password, firstName, lastName) {
    const response = await fetch(`${this.baseUrl}/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password, firstName, lastName })
    });
    const data = await response.json();
    this.storeTokens(data.tokens);
    return data.user;
  }

  async login(email, password) {
    const response = await fetch(`${this.baseUrl}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    const data = await response.json();
    this.storeTokens(data.tokens);
    return data.user;
  }

  async logout() {
    const refreshToken = localStorage.getItem('refreshToken');
    await fetch(`${this.baseUrl}/revoke`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    this.clearTokens();
  }

  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    const response = await fetch(`${this.baseUrl}/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    const data = await response.json();
    this.storeTokens(data);
    return data.accessToken;
  }

  storeTokens(tokens) {
    localStorage.setItem('accessToken', tokens.accessToken);
    localStorage.setItem('refreshToken', tokens.refreshToken);
  }

  clearTokens() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  getAccessToken() {
    return localStorage.getItem('accessToken');
  }
}

// Usage
const auth = new AuthClient('https://your-auth-service/api/auth');

// Register
await auth.register('user@example.com', 'pass123', 'John', 'Doe');

// Login
await auth.login('user@example.com', 'pass123');

// Use token in requests
const accessToken = auth.getAccessToken();

// Logout
await auth.logout();
```
