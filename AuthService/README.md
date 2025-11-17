# Auth Service

A comprehensive multi-tenant authentication and authorization service built with ASP.NET Core 10.0, featuring JWT tokens, API key management, role-based access control, and an admin dashboard.

## Features

- **Multi-tenant Architecture**: Isolated user management per application
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **API Key Management**: Generate, validate, and manage API keys with rate limiting
- **Role-Based Access Control**: Granular permissions system
- **Admin Dashboard**: Web interface for user and application management
- **Session Logging**: Complete audit trail of user activities
- **Health Checks**: Monitoring endpoints for system health
- **Docker Support**: Containerized deployment ready

## Tech Stack

- **ASP.NET Core 10.0**
- **Entity Framework Core 9.0**
- **PostgreSQL** (via Npgsql)
- **JWT Bearer Authentication**
- **BCrypt** for password hashing
- **Swagger/OpenAPI** for API documentation
- **Bootstrap 5** for admin dashboard

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 16+
- Docker & Docker Compose (optional)

### Local Development with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AuthService
   ```

2. **Start services with Docker Compose**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Health Check: http://localhost:8080/health

### Manual Setup

1. **Install dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure database**
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Username=youruser;Password=yourpassword;Database=authservice"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Start the application**
   ```bash
   dotnet run
   ```

## API Endpoints

### Authentication
- `POST /auth/register` - Register new user
- `POST /auth/login` - User login
- `POST /auth/refresh` - Refresh access token
- `POST /auth/logout` - User logout
- `GET /auth/me` - Get current user info

### Application Management (Admin)
- `GET /admin/apps` - List all applications
- `POST /admin/apps` - Create application
- `GET /admin/apps/{id}` - Get application details
- `PUT /admin/apps/{id}` - Update application
- `DELETE /admin/apps/{id}` - Delete application

### User Management (Admin)
- `GET /admin/{app}/users` - List users in application
- `GET /admin/{app}/users/{id}` - Get user details
- `PUT /admin/{app}/users/{id}` - Update user
- `DELETE /admin/{app}/users/{id}` - Delete user
- `POST /admin/{app}/users/{id}/roles` - Assign role to user
- `DELETE /admin/{app}/users/{id}/roles/{role}` - Remove role from user

### API Key Management
- `GET /admin/{app}/api-keys` - List API keys
- `POST /admin/{app}/api-keys` - Create API key
- `PUT /admin/{app}/api-keys/{id}` - Update API key
- `DELETE /admin/{app}/api-keys/{id}` - Revoke API key
- `POST /internal/validate-api-key` - Validate API key

### Health & Monitoring
- `GET /health` - Overall health status
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe

## Database Schema

### Core Entities
- **Applications**: Multi-tenant application definitions
- **Users**: User accounts scoped to applications
- **Roles**: Permission roles within applications
- **UserRoles**: Many-to-many user-role relationships
- **RefreshTokens**: JWT refresh token storage
- **SessionLogs**: Audit trail of user sessions
- **ApiKeys**: API key management with scopes

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Username=user;Password=pass;Database=auth

# JWT Settings
Jwt__Key=your-super-secret-jwt-key-that-should-be-at-least-256-bits-long
Jwt__Issuer=https://auth.yourdomain.com
Jwt__Audience=your-microservices
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7

# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

### JWT Token Structure

```json
{
  "sub": "user-guid",
  "app": "application-code",
  "app_id": "application-guid",
  "email": "user@example.com",
  "roles": ["admin", "user"],
  "iat": 1731800000,
  "exp": 1731803600,
  "iss": "https://auth.yourdomain.com",
  "aud": "your-microservices"
}
```

## Testing

### HTTP Tests

Use the included `AuthService.http` file with VS Code REST Client extension:

```bash
# Install REST Client extension in VS Code
# Open AuthService.http and run individual requests
```

### Health Checks

```bash
# Overall health
curl http://localhost:8080/health

# Liveness probe
curl http://localhost:8080/health/live

# Readiness probe
curl http://localhost:8080/health/ready
```

## Deployment

### Docker Production Build

```bash
# Build production image
docker build -t authservice:latest .

# Run with environment variables
docker run -d \
  --name authservice \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Username=user;Password=pass;Database=auth" \
  -e Jwt__Key="your-production-jwt-key" \
  authservice:latest
```

### Kubernetes Deployment

```yaml
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
        image: authservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
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
```

## Security Features

- **Password Hashing**: BCrypt with salt
- **JWT Tokens**: Secure token-based authentication
- **API Key Rate Limiting**: 100 requests per minute per key
- **Session Logging**: Complete audit trail
- **Input Validation**: Comprehensive model validation
- **CORS Protection**: Configurable cross-origin policies

## Monitoring & Logging

- **Structured Logging**: Serilog integration ready
- **Health Checks**: Database connectivity and self-checks
- **Metrics**: Request counting and performance monitoring
- **Error Handling**: Comprehensive exception handling with proper HTTP status codes

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:
- Create an issue in the repository
- Check the documentation in this README
- Review the API documentation at `/swagger`

---

**Note**: This is a production-ready authentication service. For production deployment, ensure:
- Use strong, randomly generated JWT keys
- Configure proper database credentials
- Set up SSL/TLS certificates
- Implement proper monitoring and alerting
- Regular security updates and patches
