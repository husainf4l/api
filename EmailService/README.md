# EmailService

A production-ready microservice for sending emails via AWS SES (Simple Email Service) with PostgreSQL persistence, API key authentication, and Docker deployment.

## 🚀 Features

- **AWS SES Integration**: Send emails using Amazon Simple Email Service
- **API Key Authentication**: Secure endpoints with X-API-Key header validation
- **PostgreSQL Database**: Log all emails, manage templates, and track API keys
- **Docker Support**: Fully containerized with docker-compose
- **Health Monitoring**: Health check endpoint for monitoring
- **Email Logging**: Track all sent emails with success/failure status
- **Template Management**: Store and reuse email templates
- **Multiple Recipients**: Support for To, CC, and BCC
- **HTML & Plain Text**: Support for both HTML and plain text emails

## 📋 Prerequisites

- Docker & Docker Compose
- AWS Account with SES configured
- Verified email addresses/domains in AWS SES (sandbox mode)

## 🏗️ Project Structure

```
EmailService/
├── .docker/                    # Docker configuration
│   ├── Dockerfile             # Multi-stage Docker build
│   └── docker-compose.yml     # Service orchestration
├── Controllers/               # API endpoints
│   └── EmailController.cs     # Email & health endpoints
├── Data/                      # Database context
│   └── EmailDbContext.cs      # EF Core DbContext
├── docs/                      # Documentation
│   ├── API_KEY_DOCUMENTATION.md
│   ├── DOCKER_DEPLOYMENT.md
│   ├── EMAIL_API_DOCUMENTATION.md
│   └── QUICK_START.md
├── DTOs/                      # Data transfer objects
│   ├── EmailResponse.cs
│   └── SendEmailRequest.cs
├── Middleware/                # Custom middleware
│   └── ApiKeyAuthMiddleware.cs
├── Migrations/                # Database migrations
│   └── init.sql               # Initial schema
├── Models/                    # Domain models
│   ├── ApiKeyModel.cs
│   ├── EmailLog.cs
│   └── EmailTemplate.cs
├── Services/                  # Business logic
│   └── AwsSesEmailService.cs  # Email service implementation
├── .env                       # Environment variables (not in git)
├── appsettings.json           # Application configuration
├── Program.cs                 # Application entry point
└── README.md                  # This file
```

## 🚀 Quick Start

### 1. Configure Environment Variables

Create `.env` file in the EmailService root:

```env
AWS_ACCESS_KEY_ID=your_access_key
AWS_SECRET_ACCESS_KEY=your_secret_key
AWS_REGION=me-central-1
API_KEY=your-secure-api-key
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=emailservice;Username=postgres;Password=postgres
```

### 2. Start Services

```bash
cd /home/husain/api/EmailService/.docker
docker-compose up -d
```

### 3. Verify Health

```bash
curl http://localhost:5102/api/email/health
```

Expected response:
```json
{"status":"Email service is healthy"}
```

### 4. Send an Email

```bash
curl -X POST http://localhost:5102/api/email/send \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secure-api-key" \
  -d '{
    "from": "noreply@aqlaan.com",
    "to": ["recipient@example.com"],
    "subject": "Test Email",
    "body": "Hello from EmailService!",
    "isHtml": false
  }'
```

## 📡 API Endpoints

### Health Check
```http
GET /api/email/health
```
No authentication required.

### Send Email
```http
POST /api/email/send
Content-Type: application/json
X-API-Key: your-api-key
```

**Request Body:**
```json
{
  "from": "sender@domain.com",
  "to": ["recipient@example.com"],
  "cc": ["cc@example.com"],          // Optional
  "bcc": ["bcc@example.com"],        // Optional
  "subject": "Email Subject",
  "body": "Email body content",
  "isHtml": false
}
```

**Response:**
```json
{
  "success": true,
  "message": "Email sent successfully",
  "messageId": "011d019a9224c2c1-..."
}
```

## 🗄️ Database Schema

### email_logs
Tracks all sent emails with status and metadata.

### email_templates
Stores reusable email templates.

### api_keys
Manages API keys with usage tracking.

## 🔒 Security

- **API Key Authentication**: All email endpoints require valid X-API-Key header
- **Environment Variables**: Sensitive credentials stored in .env (excluded from git)
- **AWS SES Sandbox**: Requires verified email addresses until production access granted
- **Database Logging**: Full audit trail of all email operations

## 🐳 Docker Commands

```bash
# Start services
cd .docker && docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f emailservice

# Rebuild after code changes
docker-compose up -d --build

# Check status
docker-compose ps
```

## 📚 Documentation

- [API Key Documentation](docs/API_KEY_DOCUMENTATION.md)
- [Docker Deployment Guide](docs/DOCKER_DEPLOYMENT.md)
- [Email API Documentation](docs/EMAIL_API_DOCUMENTATION.md)
- [Quick Start Guide](docs/QUICK_START.md)

## 🛠️ Development

### Local Development (without Docker)

```bash
# Install dependencies
dotnet restore

# Update appsettings.Development.json with your database connection

# Run migrations
dotnet ef database update

# Run the application
dotnet run
```

The service will be available at `http://localhost:5189`

### Adding New Features

1. Create models in `Models/`
2. Add business logic in `Services/`
3. Expose via controllers in `Controllers/`
4. Update database schema in `Migrations/`

## 🔧 Configuration

### AWS SES Setup

1. Create AWS account and configure SES
2. Verify sender email addresses/domains
3. Request production access (remove sandbox limitations)
4. Generate IAM access keys with SES permissions

### Database Configuration

PostgreSQL is automatically initialized with the schema in `Migrations/init.sql`. The database includes:
- Email logging with indexing
- Template management
- API key tracking with usage metrics

## 📊 Monitoring

- **Health Endpoint**: `/api/email/health` for uptime checks
- **Database Logs**: Query `email_logs` table for delivery status
- **API Key Usage**: Track usage via `api_keys` table
- **Docker Logs**: `docker-compose logs -f emailservice`

## 🚨 Troubleshooting

### Email not sending
- Verify AWS credentials in .env
- Check email addresses are verified in SES console
- Review `email_logs` table for error messages

### Container issues
```bash
docker-compose down
docker-compose up -d --build
```

### Database connection errors
- Ensure postgres container is healthy: `docker-compose ps`
- Check connection string in .env

## 📝 License

Private - Internal Use Only

## 👤 Contact

For questions or support, contact the development team.

---

**Built with**: ASP.NET Core 10.0 | AWS SES | PostgreSQL 17 | Docker
