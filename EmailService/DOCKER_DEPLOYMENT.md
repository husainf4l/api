# EmailService - Docker Deployment Guide

## 🐳 Docker Setup

### Prerequisites
- Docker installed
- Docker Compose installed

---

## 🚀 Quick Start

### 1. Build and Run with Docker Compose

```bash
# Build and start the service
docker-compose up -d

# View logs
docker-compose logs -f emailservice

# Stop the service
docker-compose down
```

### 2. Build Docker Image Manually

```bash
# Build the image
docker build -t emailservice:latest -f EmailService/Dockerfile .

# Run the container
docker run -d \
  -p 5189:5189 \
  -e AWS_ACCESS_KEY_ID=your-access-key \
  -e AWS_SECRET_ACCESS_KEY=your-secret-key \
  -e AWS_REGION=me-central-1 \
  -e ApiSettings__ApiKey=your-api-key \
  --name emailservice \
  emailservice:latest
```

---

## 🔧 Configuration

### Environment Variables

The service requires the following environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `AWS_ACCESS_KEY_ID` | AWS Access Key | `AKIA...` |
| `AWS_SECRET_ACCESS_KEY` | AWS Secret Key | `secret...` |
| `AWS_REGION` | AWS Region | `me-central-1` |
| `ApiSettings__ApiKey` | API Key for authentication | `your-secure-key` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Environment | `Production` |

### Using .env File

Create or use `EmailService/.env`:

```env
AWS_ACCESS_KEY_ID=AKIA46ALPORVS4J45A54
AWS_SECRET_ACCESS_KEY=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
AWS_REGION=me-central-1
API_KEY=dev-email-service-key-2024
```

---

## 📋 Docker Commands

### View Running Containers
```bash
docker ps
```

### View Logs
```bash
# Follow logs
docker logs -f emailservice

# Last 100 lines
docker logs --tail 100 emailservice
```

### Stop Container
```bash
docker stop emailservice
```

### Start Container
```bash
docker start emailservice
```

### Remove Container
```bash
docker rm -f emailservice
```

### Remove Image
```bash
docker rmi emailservice:latest
```

---

## 🧪 Test the Dockerized Service

```bash
# Health check
curl http://localhost:5189/api/email/health

# Send test email
curl -X POST http://localhost:5189/api/email/send \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{
    "to": "husain.f4l@gmail.com",
    "subject": "Docker Test",
    "body": "Email from Dockerized EmailService",
    "isHtml": false
  }'
```

---

## 🔄 Update and Rebuild

```bash
# Stop and remove existing container
docker-compose down

# Rebuild and start
docker-compose up -d --build

# Or manually
docker stop emailservice
docker rm emailservice
docker build -t emailservice:latest -f EmailService/Dockerfile .
docker run -d -p 5189:5189 --env-file EmailService/.env --name emailservice emailservice:latest
```

---

## 📦 Production Deployment

### Using Docker Compose (Recommended)

1. Update `EmailService/.env` with production credentials
2. Update `appsettings.json` with production API key
3. Run:

```bash
docker-compose up -d
```

### Push to Docker Registry

```bash
# Tag the image
docker tag emailservice:latest your-registry/emailservice:1.0.0

# Push to registry
docker push your-registry/emailservice:1.0.0
```

---

## 🐋 Docker Image Details

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:10.0`
- **Exposed Port**: `5189`
- **Working Directory**: `/app`

---

## 🔍 Troubleshooting

### Container not starting

```bash
# Check logs
docker logs emailservice

# Check if port is in use
sudo lsof -i :5189
```

### Environment variables not loaded

```bash
# Verify environment variables in container
docker exec emailservice env | grep AWS
docker exec emailservice env | grep ApiSettings
```

### Permission issues

```bash
# Run with appropriate permissions
docker-compose up -d --force-recreate
```

---

## 📊 Monitoring

### Check container status
```bash
docker ps -a
```

### View resource usage
```bash
docker stats emailservice
```

### Inspect container
```bash
docker inspect emailservice
```

---

## ✅ Verification Checklist

- [ ] Docker image builds successfully
- [ ] Container starts without errors
- [ ] Health endpoint responds: `http://localhost:5189/api/email/health`
- [ ] Email sending works with valid API key
- [ ] Environment variables are loaded correctly
- [ ] Logs are accessible via `docker logs`

---

**Your EmailService is now containerized and ready for deployment!** 🐳
