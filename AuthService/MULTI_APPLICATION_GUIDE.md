# Multi-Application Authentication System

## Overview
The AuthService now supports **multi-application authentication**, allowing you to track which application users are signing in from. This is essential for microservices architecture where multiple applications use the same authentication service.

## Key Features

### 1. **Application Management**
- Register applications with unique `clientId` and `clientSecret`
- Track allowed origins (CORS)
- Define application-specific scopes
- Manage active/inactive applications

### 2. **Session Tracking**
- Track which application each user session belongs to
- Monitor user activity per application
- IP address and device information logging
- Session lifecycle management (login, activity, logout)

### 3. **Token Management**
- Refresh tokens are now associated with specific applications
- Prevents token reuse across different applications
- Enhanced security through application-specific tokens

## Database Schema

### New Tables

#### **Applications**
```sql
- Id (UUID, Primary Key)
- Name (string, required)
- ClientId (string, unique, required)
- ClientSecret (string, hashed, required)
- Description (string, optional)
- IsActive (boolean, default: true)
- AllowedOrigins (string[], CORS origins)
- AllowedScopes (string[], application permissions)
- CreatedAt (timestamp)
- UpdatedAt (timestamp)
```

#### **UserSessions**
```sql
- Id (UUID, Primary Key)
- UserId (UUID, Foreign Key → Users)
- ApplicationId (UUID, Foreign Key → Applications)
- IpAddress (string, required)
- UserAgent (string, optional)
- DeviceInfo (string, optional)
- LoginAt (timestamp)
- LastActivityAt (timestamp)
- LogoutAt (timestamp)
- IsActive (boolean, default: true)
```

#### **RefreshTokens** (Updated)
- Added `ApplicationId` (nullable for backwards compatibility)
- Tokens are now application-specific

## API Endpoints

### Application Management

#### Register Application
```http
POST /api/applications/register
Content-Type: application/json

{
  "name": "Email Service",
  "clientId": "email-service-client",
  "clientSecret": "email-service-secret-123",
  "description": "Main email marketing service",
  "allowedOrigins": ["http://localhost:3000"],
  "allowedScopes": ["email:send", "email:read"]
}
```

#### Get All Applications
```http
GET /api/applications
```

#### Get Application Sessions
```http
GET /api/applications/{applicationId}/sessions
```

### Authentication (Application-Aware)

#### Register with Application Context
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!@#",
  "firstName": "John",
  "lastName": "Doe",
  "clientId": "email-service-client"  // Optional but recommended
}
```

#### Login with Application Context
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!@#",
  "clientId": "email-service-client"  // Optional but recommended
}
```

## Registered Applications

### 1. Email Service
- **Client ID**: `email-service-client`
- **Client Secret**: `email-service-secret-123`
- **Description**: Main email marketing and campaign service
- **Allowed Origins**: 
  - `http://localhost:3000`
  - `http://localhost:5173`
  - `https://email.yourdomain.com`
- **Scopes**: `email:send`, `email:read`, `campaigns:manage`

### 2. Admin Dashboard
- **Client ID**: `admin-dashboard-client`
- **Client Secret**: `dashboard-secret-123`
- **Description**: Administrative dashboard for managing services
- **Allowed Origins**:
  - `http://localhost:5201`
  - `https://admin.yourdomain.com`
- **Scopes**: `admin:read`, `admin:write`, `users:manage`

## Benefits

### Security
✅ Application-specific tokens prevent cross-application token reuse
✅ Track suspicious activity across different applications
✅ Revoke access for specific applications without affecting others

### Analytics
✅ Monitor which applications are most used
✅ Track user behavior per application
✅ Identify inactive applications

### Compliance
✅ Audit trail shows which application accessed what data
✅ Session history per application
✅ Data access transparency

### Multi-Tenancy
✅ Same user can have different sessions in different applications
✅ Application-specific permissions (scopes)
✅ Independent session management

## Usage Example

### For Your Email Service

```javascript
// When users log in from your email service frontend
const response = await fetch('http://localhost:5067/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123',
    clientId: 'email-service-client'  // Identifies the email service
  })
});

const { tokens, user } = await response.json();
// Store tokens and use for subsequent requests
```

### For Your Dashboard

```javascript
// When admins log in to the dashboard
const response = await fetch('http://localhost:5067/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'admin@example.com',
    password: 'adminpass123',
    clientId: 'admin-dashboard-client'  // Identifies the dashboard
  })
});
```

## Monitoring

### Check Active Sessions for an Application
```bash
curl http://localhost:5067/api/applications/{applicationId}/sessions
```

This returns all active sessions for that specific application, including:
- User information
- Login time
- Last activity
- IP address and device info

## Next Steps

1. **Add Application Validation Middleware**
   - Validate `clientId` and `clientSecret` for sensitive operations
   - Enforce application-specific rate limits

2. **Implement Scope-Based Authorization**
   - Check if user has required scopes for the application
   - Add `[RequireScope("email:send")]` attributes to endpoints

3. **Add Application Analytics Dashboard**
   - Show usage statistics per application
   - Track popular features
   - Monitor session durations

4. **Implement Application-Level Settings**
   - Session timeout per application
   - Token expiry per application
   - Custom authentication flows

## Database Migration Applied

```bash
Migration: 20251117104802_AddApplicationsAndSessions
Status: ✅ Applied Successfully

Tables Created:
- Applications
- UserSessions

Tables Modified:
- RefreshTokens (added ApplicationId column)
```

## Testing

All endpoints are working correctly:
- ✅ Application registration
- ✅ Application listing
- ✅ Login with clientId
- ✅ Session tracking
- ✅ Backwards compatibility (clientId is optional)

Your AuthService is now ready to handle authentication for multiple applications! 🎉
