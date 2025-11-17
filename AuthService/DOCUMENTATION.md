# AuthService Complete Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Getting Started](#getting-started)
4. [API Reference](#api-reference)
5. [Authentication & Authorization](#authentication--authorization)
6. [Multi-Tenant Usage](#multi-tenant-usage)
7. [API Key Management](#api-key-management)
8. [Two-Factor Authentication](#two-factor-authentication)
9. [External Login (OAuth)](#external-login-oauth)
10. [Admin Dashboard](#admin-dashboard)
11. [Integration Guide](#integration-guide)
12. [Troubleshooting](#troubleshooting)

---

## Overview

AuthService is a production-ready, multi-tenant authentication and authorization service built with ASP.NET Core 10.0. It provides comprehensive user management, JWT-based authentication, API key management, and role-based access control.

### Key Features

- **Multi-Tenant Architecture**: Isolated user management per application
- **JWT Authentication**: Secure token-based auth with refresh tokens
- **API Key Management**: Generate and validate API keys with rate limiting
- **Role-Based Access Control (RBAC)**: Granular permission management
- **Two-Factor Authentication**: TOTP-based 2FA with backup codes
- **External Login**: OAuth integration (Google, GitHub, Microsoft)
- **Email Verification**: Email confirmation and password reset
- **Session Logging**: Complete audit trail
- **Admin Dashboard**: Web-based management interface
- **Health Checks**: Production-ready monitoring endpoints

### Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL 16+ (via Entity Framework Core 10.0)
- **Authentication**: JWT Bearer tokens
- **Password Hashing**: BCrypt
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker & Docker Compose

---

## Architecture

### System Architecture

```
┌─────────────────┐
│   Client Apps   │
│  (Web, Mobile)  │
└────────┬────────┘
         │
         ↓
┌─────────────────────────────────┐
│      AuthService API            │
│  ┌───────────────────────────┐  │
│  │   Controllers Layer       │  │
│  │  - Auth Controller        │  │
│  │  - Users Controller       │  │
│  │  - Apps Controller        │  │
│  │  - API Keys Controller    │  │
│  └───────────┬───────────────┘  │
│              ↓                   │
│  ┌───────────────────────────┐  │
│  │   Services Layer          │  │
│  │  - Auth Service           │  │
│  │  - User Service           │  │
│  │  - JWT Token Service      │  │
│  │  - API Key Service        │  │
│  │  - 2FA Service            │  │
│  │  - External Login Service │  │
│  └───────────┬───────────────┘  │
│              ↓                   │
│  ┌───────────────────────────┐  │
│  │   Data Layer (EF Core)    │  │
│  │  - AuthDbContext          │  │
│  └───────────┬───────────────┘  │
└──────────────┼───────────────────┘
               ↓
    ┌──────────────────┐
    │   PostgreSQL DB  │
    └──────────────────┘
```

### Database Schema

```
Applications (Multi-tenant container)
├── Users (Scoped to application)
│   ├── UserRoles (Many-to-many)
│   ├── RefreshTokens
│   ├── SessionLogs
│   ├── EmailTokens
│   └── UserExternalLogins
├── Roles (Scoped to application)
└── ApiKeys (Scoped to application)
```

---

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 16+
- Docker (optional, recommended)

### Installation

#### Option 1: Docker Compose (Recommended)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AuthService
   ```

2. **Configure environment variables**
   Edit `docker-compose.yml` or create `.env` file:
   ```env
   POSTGRES_PASSWORD=your_secure_password
   JWT_KEY=your-super-secret-jwt-key-at-least-256-bits
   ```

3. **Start services**
   ```bash
   docker-compose up -d
   ```

4. **Access the service**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Health: http://localhost:8080/health

#### Option 2: Local Development

1. **Install dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure database**
   
   Update `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=your_password;Database=authservice"
     },
     "Jwt": {
       "Key": "your-super-secret-jwt-key-that-should-be-at-least-256-bits-long",
       "Issuer": "https://auth.yourdomain.com",
       "Audience": "your-microservices",
       "AccessTokenExpirationMinutes": 15,
       "RefreshTokenExpirationDays": 7
     }
   }
   ```

3. **Run migrations**
   ```bash
   dotnet ef database update
   ```

4. **Start the application**
   ```bash
   dotnet run
   ```

---

## API Reference

### Base URL
```
http://localhost:8080
```

### Response Format

All API responses follow this structure:

**Success Response:**
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Response data here
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Error message",
  "errors": ["Validation error 1", "Validation error 2"]
}
```

### Authentication Endpoints

#### 1. Register User

**POST** `/auth/register`

Creates a new user account in a specific application.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd",
  "applicationCode": "myapp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "7f3d8e9a-1b2c-4d5e-6f7a-8b9c0d1e2f3a",
    "expiresIn": 900,
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "applicationCode": "myapp",
      "roles": []
    }
  }
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:8080/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecureP@ssw0rd",
    "applicationCode": "myapp"
  }'
```

---

#### 2. Login User

**POST** `/auth/login`

Authenticates a user and returns JWT tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd",
  "applicationCode": "myapp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "7f3d8e9a-1b2c-4d5e-6f7a-8b9c0d1e2f3a",
    "expiresIn": 900,
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "applicationCode": "myapp",
      "roles": ["user"]
    }
  }
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecureP@ssw0rd",
    "applicationCode": "myapp"
  }'
```

---

#### 3. Refresh Token

**POST** `/token/refresh`

Refreshes an expired access token using a refresh token.

**Request Body:**
```json
{
  "refreshToken": "7f3d8e9a-1b2c-4d5e-6f7a-8b9c0d1e2f3a"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "8g4e9f0b-2c3d-5e6f-7g8b-9c0d1e2f3g4b",
    "expiresIn": 900
  }
}
```

---

#### 4. Get Current User

**GET** `/auth/me`

Retrieves information about the currently authenticated user.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "application_id": "660f9500-f30c-52e5-b827-557766551111",
    "application_code": "myapp",
    "application_name": "My Application",
    "roles": ["user", "admin"],
    "is_email_verified": true,
    "created_at": "2025-11-17T10:00:00Z",
    "last_login_at": "2025-11-17T12:30:00Z"
  }
}
```

**cURL Example:**
```bash
curl -X GET http://localhost:8080/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

#### 5. Logout

**POST** `/auth/logout`

Invalidates the refresh token, logging out the user.

**Request Body:**
```json
{
  "refreshToken": "7f3d8e9a-1b2c-4d5e-6f7a-8b9c0d1e2f3a"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User logged out successfully"
}
```

---

### Password Management

#### 6. Change Password (Authenticated)

**POST** `/auth/change-password`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Request Body:**
```json
{
  "currentPassword": "OldP@ssw0rd",
  "newPassword": "NewSecureP@ssw0rd",
  "applicationCode": "myapp"
}
```

---

#### 7. Forgot Password

**POST** `/auth/forgot-password`

**Request Body:**
```json
{
  "email": "user@example.com",
  "applicationCode": "myapp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "If the email exists, a password reset link has been sent"
}
```

---

#### 8. Reset Password

**POST** `/auth/reset-password`

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "abc123def456",
  "newPassword": "NewSecureP@ssw0rd"
}
```

---

### Email Verification

#### 9. Verify Email

**POST** `/auth/verify-email`

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "xyz789abc123"
}
```

---

#### 10. Resend Verification Email

**POST** `/auth/resend-verification`

**Request Body:**
```json
{
  "email": "user@example.com",
  "applicationCode": "myapp"
}
```

---

### Application Management (Admin)

#### 11. List Applications

**GET** `/admin/apps`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "660f9500-f30c-52e5-b827-557766551111",
      "name": "My Application",
      "code": "myapp",
      "createdAt": "2025-11-01T10:00:00Z",
      "isActive": true,
      "userCount": 150,
      "apiKeyCount": 5
    }
  ]
}
```

---

#### 12. Create Application

**POST** `/admin/apps`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Request Body:**
```json
{
  "name": "My New App",
  "code": "mynewapp",
  "description": "Description of my application"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Application created successfully",
  "data": {
    "id": "770a0611-g41d-63f6-c938-668877662222",
    "name": "My New App",
    "code": "mynewapp",
    "createdAt": "2025-11-17T12:00:00Z",
    "isActive": true
  }
}
```

---

#### 13. Get Application Details

**GET** `/admin/apps/{id}`

**Headers:**
```
Authorization: Bearer <admin_token>
```

---

### User Management (Admin)

#### 14. List Users in Application

**GET** `/admin/{app}/users?page=1&pageSize=50`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "isEmailVerified": true,
      "isActive": true,
      "roles": ["user"],
      "createdAt": "2025-11-01T10:00:00Z",
      "lastLoginAt": "2025-11-17T12:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "total": 150
  }
}
```

---

#### 15. Get User Details

**GET** `/admin/{app}/users/{id}`

**Headers:**
```
Authorization: Bearer <admin_token>
```

---

#### 16. Update User

**PUT** `/admin/{app}/users/{id}`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Request Body:**
```json
{
  "email": "newemail@example.com",
  "isActive": true,
  "isEmailVerified": true
}
```

---

#### 17. Delete User

**DELETE** `/admin/{app}/users/{id}`

**Headers:**
```
Authorization: Bearer <admin_token>
```

---

### API Key Management

#### 18. List API Keys

**GET** `/admin/{app}/api-keys`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "880b1722-h52e-74g7-d049-779988773333",
      "name": "Production API Key",
      "keyPrefix": "ak_prod_",
      "scopes": ["read", "write"],
      "expiresAt": "2026-11-17T00:00:00Z",
      "isActive": true,
      "createdAt": "2025-11-01T10:00:00Z"
    }
  ]
}
```

---

#### 19. Create API Key

**POST** `/admin/{app}/api-keys`

**Headers:**
```
Authorization: Bearer <admin_token>
```

**Request Body:**
```json
{
  "name": "Production API Key",
  "scopes": ["read", "write"],
  "expiresInDays": 365
}
```

**Response:**
```json
{
  "success": true,
  "message": "API Key created successfully",
  "data": {
    "id": "880b1722-h52e-74g7-d049-779988773333",
    "name": "Production API Key",
    "key": "ak_prod_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz",
    "scopes": ["read", "write"],
    "expiresAt": "2026-11-17T00:00:00Z",
    "createdAt": "2025-11-17T12:00:00Z",
    "warning": "Save this key securely. It will not be shown again."
  }
}
```

---

#### 20. Validate API Key

**POST** `/internal/validate-api-key`

**Request Body:**
```json
{
  "apiKey": "ak_prod_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "isValid": true,
    "applicationId": "660f9500-f30c-52e5-b827-557766551111",
    "applicationCode": "myapp",
    "scopes": ["read", "write"],
    "expiresAt": "2026-11-17T00:00:00Z"
  }
}
```

---

#### 21. Revoke API Key

**DELETE** `/admin/{app}/api-keys/{id}`

**Headers:**
```
Authorization: Bearer <admin_token>
```

---

### Two-Factor Authentication

#### 22. Setup 2FA

**POST** `/auth/2fa/setup`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Response:**
```json
{
  "success": true,
  "message": "Two-factor authentication setup initiated",
  "data": {
    "success": true,
    "secret": "JBSWY3DPEHPK3PXP",
    "qrCodeUri": "otpauth://totp/AuthService:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=AuthService",
    "qrCodeImageBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
    "backupCodes": [
      "12345678",
      "87654321",
      "13579246",
      "24681357",
      "98765432"
    ]
  }
}
```

---

#### 23. Enable 2FA

**POST** `/auth/2fa/enable`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Request Body:**
```json
{
  "verificationCode": "123456"
}
```

---

#### 24. Disable 2FA

**POST** `/auth/2fa/disable`

**Headers:**
```
Authorization: Bearer <access_token>
```

---

#### 25. Get 2FA Status

**GET** `/auth/2fa/status`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Response:**
```json
{
  "success": true,
  "data": {
    "isEnabled": true,
    "isConfigured": true,
    "hasBackupCodes": true
  }
}
```

---

### External Login (OAuth)

#### 26. Initiate External Login

**POST** `/auth/external-login`

**Request Body:**
```json
{
  "provider": "google",
  "applicationCode": "myapp"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Redirect to google for authentication",
  "data": {
    "provider": "google",
    "authorizationUrl": "https://accounts.google.com/o/oauth2/v2/auth?client_id=..."
  }
}
```

---

#### 27. Get Linked External Logins

**GET** `/auth/external-logins`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "provider": "google",
      "providerUserId": "108765432109876543210",
      "linkedAt": "2025-11-01T10:00:00Z"
    },
    {
      "provider": "github",
      "providerUserId": "12345678",
      "linkedAt": "2025-11-10T14:30:00Z"
    }
  ]
}
```

---

#### 28. Unlink External Login

**POST** `/auth/unlink-external-login`

**Headers:**
```
Authorization: Bearer <access_token>
```

**Request Body:**
```json
{
  "provider": "google"
}
```

---

### Health & Monitoring

#### 29. Health Check

**GET** `/health`

**Response:**
```json
{
  "status": "Healthy",
  "results": {
    "database": {
      "status": "Healthy"
    },
    "self": {
      "status": "Healthy"
    }
  }
}
```

---

#### 30. Liveness Probe

**GET** `/health/live`

---

#### 31. Readiness Probe

**GET** `/health/ready`

---

## Authentication & Authorization

### JWT Token Structure

The access token is a JWT with the following claims:

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "app": "myapp",
  "app_id": "660f9500-f30c-52e5-b827-557766551111",
  "app_name": "My Application",
  "roles": ["user", "admin"],
  "iat": 1731850000,
  "exp": 1731850900,
  "iss": "https://auth.yourdomain.com",
  "aud": "your-microservices"
}
```

### Using JWT Tokens

Include the access token in the Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Expiration

- **Access Token**: 15 minutes (default)
- **Refresh Token**: 7 days (default)

Use the refresh token to obtain a new access token before it expires.

---

## Multi-Tenant Usage

### Understanding Applications

Each "Application" in AuthService represents a separate tenant with:
- Isolated user base
- Separate roles
- Independent API keys
- Isolated session logs

### Creating an Application

**Step 1**: Create an application (admin only)
```bash
curl -X POST http://localhost:8080/admin/apps \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Mobile App",
    "code": "mobile-app",
    "description": "iOS and Android application"
  }'
```

**Step 2**: Register users for this application
```bash
curl -X POST http://localhost:8080/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecureP@ssw0rd",
    "applicationCode": "mobile-app"
  }'
```

### Application Isolation

- Users registered to "mobile-app" **cannot** log in to "web-app"
- Each application has its own role definitions
- API keys are scoped to specific applications

---

## API Key Management

### Creating API Keys

API keys allow server-to-server authentication without user credentials.

**Create an API Key:**
```bash
curl -X POST http://localhost:8080/admin/myapp/api-keys \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Production Server",
    "scopes": ["read", "write"],
    "expiresInDays": 365
  }'
```

**Response includes the full key (shown only once):**
```json
{
  "key": "ak_prod_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz"
}
```

### Using API Keys

Include the API key in requests:

```http
X-API-Key: ak_prod_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz
```

Or validate programmatically:

```bash
curl -X POST http://localhost:8080/internal/validate-api-key \
  -H "Content-Type: application/json" \
  -d '{
    "apiKey": "ak_prod_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz"
  }'
```

### API Key Scopes

Define what operations the API key can perform:
- `read`: Read-only access
- `write`: Create/update operations
- `delete`: Delete operations
- `admin`: Administrative access

---

## Two-Factor Authentication

### Setup Flow

**1. User initiates 2FA setup:**
```bash
curl -X POST http://localhost:8080/auth/2fa/setup \
  -H "Authorization: Bearer <access_token>"
```

**2. User receives QR code and secret:**
- Scan QR code with authenticator app (Google Authenticator, Authy, etc.)
- Or manually enter the secret key

**3. User enters verification code to enable:**
```bash
curl -X POST http://localhost:8080/auth/2fa/enable \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "verificationCode": "123456"
  }'
```

### Backup Codes

When setting up 2FA, users receive backup codes. Store these securely:
```
12345678
87654321
13579246
24681357
98765432
```

Each code can be used once if the authenticator is unavailable.

### Login with 2FA

When 2FA is enabled, the login flow changes:

1. **Initial login** with email/password
2. **Verify 2FA** code required before receiving tokens
3. Use authenticator code or backup code

---

## External Login (OAuth)

### Supported Providers

- Google
- GitHub
- Microsoft (configurable)

### Configuration

Update `appsettings.json`:

```json
{
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    }
  }
}
```

### OAuth Flow

**1. Initiate login:**
```bash
curl -X POST http://localhost:8080/auth/external-login \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "google",
    "applicationCode": "myapp"
  }'
```

**2. Redirect user to authorization URL**

**3. OAuth provider redirects to callback URL:**
```
http://localhost:8080/auth/external-callback/google?code=...
```

**4. System creates/links account and returns JWT tokens**

### Linking Multiple Accounts

Users can link multiple OAuth providers to one account:

```bash
curl -X POST http://localhost:8080/auth/link-external-login \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "github"
  }'
```

---

## Admin Dashboard

Access the web-based admin dashboard at:

```
http://localhost:8080/Dashboard
```

### Features

- **Application Management**: View and manage applications
- **User Management**: Browse users, edit profiles, assign roles
- **API Key Management**: Create and revoke API keys
- **Session Logs**: View authentication history
- **Statistics**: User count, active sessions, recent activity

### Dashboard Routes

- `/Dashboard` - Main dashboard
- `/Applications` - Application list
- `/Applications/Details?id={id}` - Application details
- `/Users?app={code}` - User list for application
- `/Users/Details?app={code}&id={id}` - User details
- `/ApiKeys?app={code}` - API key management

---

## Integration Guide

### Integrating with Your Application

#### Step 1: Register Your Application

```bash
curl -X POST http://localhost:8080/admin/apps \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Web App",
    "code": "webapp",
    "description": "Main web application"
  }'
```

#### Step 2: Implement User Registration

```javascript
// Frontend code (JavaScript)
async function register(email, password) {
  const response = await fetch('http://localhost:8080/auth/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      email: email,
      password: password,
      applicationCode: 'webapp'
    })
  });
  
  const data = await response.json();
  
  if (data.success) {
    // Store tokens
    localStorage.setItem('accessToken', data.data.accessToken);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return true;
  }
  
  return false;
}
```

#### Step 3: Implement User Login

```javascript
async function login(email, password) {
  const response = await fetch('http://localhost:8080/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      email: email,
      password: password,
      applicationCode: 'webapp'
    })
  });
  
  const data = await response.json();
  
  if (data.success) {
    localStorage.setItem('accessToken', data.data.accessToken);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return true;
  }
  
  return false;
}
```

#### Step 4: Use Access Token

```javascript
async function fetchUserData() {
  const accessToken = localStorage.getItem('accessToken');
  
  const response = await fetch('http://localhost:8080/auth/me', {
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });
  
  const data = await response.json();
  return data.data;
}
```

#### Step 5: Handle Token Refresh

```javascript
async function refreshAccessToken() {
  const refreshToken = localStorage.getItem('refreshToken');
  
  const response = await fetch('http://localhost:8080/token/refresh', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      refreshToken: refreshToken
    })
  });
  
  const data = await response.json();
  
  if (data.success) {
    localStorage.setItem('accessToken', data.data.accessToken);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return true;
  }
  
  // Refresh token expired, redirect to login
  return false;
}
```

### Backend Integration (.NET)

```csharp
// Validate JWT token in your microservice
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _jwtKey;

    public JwtAuthenticationMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _jwtKey = config["Jwt:Key"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "https://auth.yourdomain.com",
                    ValidateAudience = true,
                    ValidAudience = "your-microservices",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == "sub").Value;
                var roles = jwtToken.Claims.Where(x => x.Type == "roles").Select(x => x.Value).ToList();

                // Attach user to context
                context.Items["UserId"] = userId;
                context.Items["Roles"] = roles;
            }
            catch
            {
                // Token validation failed
            }
        }

        await _next(context);
    }
}
```

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed

**Error:**
```
Database migration failed: could not connect to server
```

**Solution:**
- Verify PostgreSQL is running: `docker ps` or `systemctl status postgresql`
- Check connection string in `appsettings.json`
- Ensure firewall allows port 5432

#### 2. JWT Validation Fails

**Error:**
```
{
  "success": false,
  "message": "Invalid or expired token"
}
```

**Solutions:**
- Token may have expired (15 min default) - use refresh token
- JWT Key mismatch - ensure all services use same key
- Check system time synchronization

#### 3. API Key Invalid

**Error:**
```
{
  "success": false,
  "message": "Invalid API key"
}
```

**Solutions:**
- Verify API key is active and not expired
- Check API key includes correct prefix (e.g., `ak_prod_`)
- Ensure API key is for the correct application

#### 4. Email Sending Fails

**Error:**
```
Error sending verification email
```

**Solution:**
- In development, emails log to console by default
- For production, configure SMTP settings in `EmailService`
- Check email configuration in `appsettings.json`

#### 5. Migration Issues

**Error:**
```
The model for context 'AuthDbContext' has pending changes
```

**Solution:**
```bash
# Create a new migration
dotnet ef migrations add FixPendingChanges

# Apply migration
dotnet ef database update
```

### Debugging

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### Performance Issues

If experiencing slow response times:

1. **Check database indexes:**
   ```sql
   SELECT * FROM pg_stat_user_indexes WHERE schemaname = 'public';
   ```

2. **Monitor database queries:**
   - Enable EF Core logging
   - Look for N+1 query problems
   - Add indexes to frequently queried columns

3. **Check connection pool:**
   - Increase max pool size in connection string
   - Monitor active connections

---

## Production Deployment

### Environment Variables

```bash
# Required
ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Username=user;Password=pass;Database=authservice"
Jwt__Key="production-secret-key-at-least-256-bits-long"
Jwt__Issuer="https://auth.yourproduction.com"
Jwt__Audience="production-microservices"

# Optional
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7
```

### Security Checklist

- [ ] Use strong, randomly generated JWT key (256+ bits)
- [ ] Enable HTTPS (TLS 1.2+)
- [ ] Configure proper CORS policies
- [ ] Use environment variables for secrets
- [ ] Enable rate limiting
- [ ] Configure logging and monitoring
- [ ] Set up database backups
- [ ] Use read-only database user for non-admin operations
- [ ] Enable audit logging
- [ ] Regular security updates

### Docker Production

```bash
# Build production image
docker build -t authservice:1.0.0 -f Dockerfile .

# Run with production settings
docker run -d \
  --name authservice \
  --restart unless-stopped \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Username=user;Password=pass;Database=auth" \
  -e Jwt__Key="your-production-jwt-key" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  authservice:1.0.0
```

### Kubernetes Deployment

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: authservice-secrets
type: Opaque
stringData:
  jwt-key: "your-production-jwt-key"
  db-connection: "Host=postgres;Port=5432;Username=user;Password=pass;Database=auth"

---

apiVersion: apps/v1
kind: Deployment
metadata:
  name: authservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: authservice
  template:
    metadata:
      labels:
        app: authservice
    spec:
      containers:
      - name: authservice
        image: authservice:1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: authservice-secrets
              key: db-connection
        - name: Jwt__Key
          valueFrom:
            secretKeyRef:
              name: authservice-secrets
              key: jwt-key
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

---

apiVersion: v1
kind: Service
metadata:
  name: authservice
spec:
  selector:
    app: authservice
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

---

## Support & Resources

### Documentation
- Swagger UI: http://localhost:8080/swagger
- This documentation: `/DOCUMENTATION.md`
- README: `/README.md`

### Logging
- Development: Console output
- Production: Configure structured logging (Serilog recommended)

### Monitoring
- Health checks: `/health`, `/health/live`, `/health/ready`
- Metrics: Integrate with Prometheus/Grafana
- Tracing: OpenTelemetry support ready

---

**Last Updated**: November 17, 2025  
**Version**: 1.0.0  
**Framework**: .NET 10.0
