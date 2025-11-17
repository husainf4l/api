# Authentication Microservice

A production-ready authentication microservice built with ASP.NET Core that provides JWT-based authentication for all your applications.

## Features

- **User Registration & Login** - Secure user authentication with email and password
- **JWT Tokens** - Access tokens with configurable expiry
- **Refresh Tokens** - Long-lived refresh tokens for seamless re-authentication
- **Token Validation** - Validate tokens across all your services
- **Token Revocation** - Logout and revoke refresh tokens
- **PostgreSQL Database** - Reliable data persistence
- **CORS Support** - Ready for microservice architecture
- **Swagger Documentation** - Interactive API documentation

## Project Structure

```
AuthService/
├── Controllers/        # API endpoints
├── Services/          # Business logic (Auth, Token, Password)
├── Repositories/      # Data access layer
├── Models/           # Database entities (User, RefreshToken)
├── DTOs/             # Data transfer objects
├── Data/             # DbContext configuration
├── Middleware/       # JWT validation middleware
└── Program.cs        # Application entry point
```

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database
- DotNetEnv for environment variables

## Configuration

### Environment Variables (.env)
```env
DATABASE_HOST=your-db-hostname
DATABASE_PORT=5432
DATABASE_USER=your-db-user
DATABASE_PASSWORD=your-db-password
DATABASE_NAME=authservice
```

> **Important:** The previous sample values contained real credentials and have been scrubbed. Rotate any deployments that may have used them and re-issue fresh secrets through your secrets manager.

### JWT Settings (appsettings.json)
```json
{
  "Jwt": {
    "Secret": "your-super-secret-jwt-key-min-32-characters-long",
    "Issuer": "AuthService",
    "Audience": "AuthServiceClients",
    "AccessTokenExpiryMinutes": "15",
    "RefreshTokenExpiryDays": "7"
  }
}
```

## Installation

1. **Restore packages**
   ```bash
   dotnet restore
   ```

2. **Update database**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

3. **Run the service**
   ```bash
   dotnet run
   ```

The service will start on `https://localhost:5001` (or configured port)

## API Endpoints

### Authentication

#### Register User
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "deviceInfo": "Chrome/Windows"
}
```

**Response:**
```json
{
  "tokens": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64string...",
    "expiresAt": "2025-11-17T12:00:00Z",
    "tokenType": "Bearer"
  },
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": false,
    "createdAt": "2025-11-17T11:00:00Z"
  }
}
```

#### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64string..."
}
```

#### Revoke Token (Logout)
```http
POST /api/auth/revoke
Content-Type: application/json

{
  "refreshToken": "base64string..."
}
```

#### Validate Token
```http
POST /api/auth/validate
Content-Type: application/json

{
  "accessToken": "eyJhbGc..."
}
```

#### Get Current User
```http
GET /api/auth/me
Authorization: Bearer eyJhbGc...
```

### Health Check
```http
GET /health
```

## Using the Service in Your Applications

### 1. Register/Login to get tokens
```javascript
// Example: Login request
const response = await fetch('https://auth-service/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

const { tokens, user } = await response.json();
// Store tokens securely
```

### 2. Use access token in your apps
```javascript
// Add to requests in other services
fetch('https://your-api/protected-endpoint', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});
```

### 3. Validate tokens in other services
```javascript
// Validate token from AuthService
const response = await fetch('https://auth-service/api/auth/validate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ accessToken })
});

const user = await response.json();
```

### 4. Refresh when token expires
```javascript
// Refresh the access token
const response = await fetch('https://auth-service/api/auth/refresh', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ refreshToken })
});

const { accessToken, refreshToken: newRefreshToken } = await response.json();
```

## Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

Remove last migration:
```bash
dotnet ef migrations remove
```

## Security Best Practices

1. **Change JWT Secret** - Update the JWT secret in production to a strong, random value
2. **Use HTTPS** - Always use HTTPS in production
3. **Secure Storage** - Store refresh tokens securely (httpOnly cookies or secure storage)
4. **Token Expiry** - Keep access token expiry short (15-60 minutes)
5. **Refresh Token Rotation** - Implemented - old refresh tokens are revoked when refreshed
6. **Environment Variables** - Never commit `.env` file with real credentials
7. **Database-Driven CORS** - Manage allowed origins through the `CorsOrigins` table so you can add/remove tenants without redeploying

## Development

### Swagger UI
Access interactive API documentation at: `https://localhost:5001/swagger`

### Logging
Logs are configured in `appsettings.json`. In development, EF Core SQL queries are logged.

### Audit Logging & Monitoring
- Authentication events (login, refresh, revoke, validate, account lock, etc.) are shipped to the SIEM endpoint defined in `Monitoring:Siem`.
- Configure `Monitoring:Siem:Endpoint` (e.g., `https://siem.company.com/events`) and optional `Monitoring:Siem:ApiKey`.
- Each event includes user, IP, device info, and contextual metadata for dashboards/alerts.

### Background Security Jobs
- A hosted scheduler runs `RefreshTokenCleanupJob` and `DormantAccountReviewJob`.
- Configure `Security:Cleanup:IntervalMinutes` to control how often jobs run.
- Adjust `Security:DormantAccountDays` to tune dormant-account detection.
- Extend by registering additional `ISecurityJob` implementations.

## Runbooks & Incident Response
Refer to `DEPLOYMENT_RUNBOOK.md` for:
- Secure deployment checklist and smoke tests
- Incident response steps (auth outage, security event)
- Recovery procedures and escalation contacts

### Managing CORS Origins
Allowed origins are stored in the database (`CorsOrigins` table) so changes can be made without code updates.

```sql
-- Allow https://app.example.com
INSERT INTO "CorsOrigins" ("Id", "Origin", "IsActive")
VALUES (gen_random_uuid(), 'https://app.example.com', true);

-- Disable an origin
UPDATE "CorsOrigins"
SET "IsActive" = false, "UpdatedAt" = NOW()
WHERE "Origin" = 'https://old.example.com';
```

After updating the table, the in-memory cache refreshes automatically within five minutes (configurable in `DatabaseCorsPolicyProvider`).

## Production Deployment

1. Update JWT secret to a strong random value
2. Configure proper CORS policies (don't use AllowAll)
3. Set appropriate token expiry times
4. Enable HTTPS
5. Set up proper logging and monitoring
6. Use connection string from secure configuration
7. Enable rate limiting
8. Consider adding email verification
9. Implement account lockout after failed attempts
10. Add API key for service-to-service calls

## License

MIT
