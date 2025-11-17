# EmailService

A production-ready microservice for sending emails via AWS SES (Simple Email Service) with GraphQL API, PostgreSQL persistence, API key authentication, and Docker deployment.

## 🚀 Features

- **GraphQL API**: Modern GraphQL interface for email operations
- **AWS SES Integration**: Send emails using Amazon Simple Email Service
- **API Key Authentication**: Secure endpoints with X-API-Key header validation
- **PostgreSQL Database**: Log all emails, manage templates, and track API keys
- **Docker Support**: Fully containerized with docker-compose
- **Health Monitoring**: Health check query for monitoring
- **Email Logging**: Track all sent emails with success/failure status
- **Template Management**: Store and reuse email templates
- **Multiple Recipients**: Support for To, CC, and BCC
- **HTML & Plain Text**: Support for both HTML and plain text emails
- **HTML Template Generator**: Built-in HTML email template generation

## 📋 Prerequisites

- Docker & Docker Compose
- AWS Account with SES configured
- Verified email addresses/domains in AWS SES (sandbox mode)

## 🏗️ Project Structure

```
EmailService/
├── Data/                      # Database context
│   └── EmailDbContext.cs      # EF Core DbContext
├── docs/                      # Documentation
│   ├── API_KEY_DOCUMENTATION.md
│   ├── DOCKER_DEPLOYMENT.md
│   ├── EMAIL_API_DOCUMENTATION.md
│   └── QUICK_START.md
├── GraphQL/                   # GraphQL schema
│   ├── EmailMutations.cs      # Mutations (sendEmail, generateHtml)
│   ├── EmailQueries.cs        # Queries (health)
│   ├── GraphQLAuthInterceptor.cs  # API key authentication
│   └── Types/                 # GraphQL types
│       ├── EmailResult.cs
│       ├── GenerateHtmlInput.cs
│       ├── GenerateHtmlResult.cs
│       ├── HealthResult.cs
│       └── SendEmailInput.cs
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
├── docker-compose.yml         # Docker orchestration
├── Dockerfile                 # Container definition
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
docker-compose up -d
```

### 3. Verify Health

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health { status service timestamp } }"}'
```

Expected response:
```json
{
  "data": {
    "health": {
      "status": "healthy",
      "service": "EmailService",
      "timestamp": "2025-11-17T12:00:00.0000000Z"
    }
  }
}
```

### 4. Send an Email

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secure-api-key" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "recipient@example.com",
        "subject": "Test Email",
        "body": "Hello from EmailService!",
        "isHtml": false
      }
    }
  }'
```

## 📡 GraphQL API

### Endpoint
```
POST http://localhost:5189/graphql
```

### Queries

#### Health Check
```graphql
query {
  health {
    status
    service
    timestamp
  }
}
```

### Mutations

#### Send Email
```graphql
mutation SendEmail($input: SendEmailInput!) {
  sendEmail(input: $input) {
    success
    message
    messageId
  }
}
```

**Variables:**
```json
{
  "input": {
    "to": "recipient@example.com",
    "cc": "optional@example.com",
    "bcc": "optional@example.com",
    "subject": "Email Subject",
    "body": "Email body or HTML content",
    "isHtml": false
  }
}
```

#### Generate HTML Template
```graphql
mutation GenerateHtml($input: GenerateHtmlInput!) {
  generateHtml(input: $input) {
    success
    html
    message
  }
}
```

**Variables:**
```json
{
  "input": {
    "title": "Welcome Email",
    "heading": "Welcome to Our Service!",
    "message": "Thank you for joining us.",
    "buttonText": "Get Started",
    "buttonUrl": "https://example.com",
    "additionalInfo": "Additional information here",
    "footerText": "Best regards, Our Team"
  }
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

- **API Key Authentication**: All mutations require valid X-API-Key header
- **GraphQL Authorization**: Built-in authorization via GraphQLAuthInterceptor
- **Environment Variables**: Sensitive credentials stored in .env (excluded from git)
- **AWS SES Sandbox**: Requires verified email addresses until production access granted
- **Database Logging**: Full audit trail of all email operations

## 🐳 Docker Commands

```bash
# Start services
docker-compose up -d

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
- [Email API Documentation](docs/EMAIL_API_DOCUMENTATION.md) - Full GraphQL reference
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

The service will be available at:
- GraphQL endpoint: `http://localhost:5189/graphql`
- GraphQL Playground (dev): `http://localhost:5189/ui/playground`

### Adding New Features

1. Create models in `Models/`
2. Add business logic in `Services/`
3. Add GraphQL types in `GraphQL/Types/`
4. Expose via queries/mutations in `GraphQL/`
5. Update database schema in `Migrations/`

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

- **Health Query**: GraphQL health query for uptime checks
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

### GraphQL errors
- Check X-API-Key header is present for mutations
- Verify query syntax in GraphQL Playground
- Review error messages in GraphQL response

## 📝 License

Private - Internal Use Only

## 👤 Contact

For questions or support, contact the development team.

---

**Built with**: ASP.NET Core 10.0 | GraphQL (HotChocolate) | AWS SES | PostgreSQL 17 | Docker
