# EmailService - Docker Deployment Guide# EmailService - Docker Deployment Guide



## 🐳 Docker Setup## 🐳 Docker Setup



### Prerequisites### Prerequisites

- Docker installed- Docker installed

- Docker Compose installed- Docker Compose installed



------



## 🚀 Quick Start## 🚀 Quick Start



### 1. Build and Run with Docker Compose### 1. Build and Run with Docker Compose



```bash```bash

# Build and start the service# Navigate to docker directory

docker-compose up -dcd .docker



# View logs# Build and start the service

docker-compose logs -f emailservicedocker-compose up -d



# Stop the service# View logs

docker-compose downdocker-compose logs -f emailservice

```

# Stop the service

### 2. Build Docker Image Manuallydocker-compose down

```

```bash

# Build the image from EmailService root### 2. Build Docker Image Manually

docker build -t emailservice:latest .

```bash

# Run the container# Build the image from EmailService root

docker run -d \docker build -t emailservice:latest .

  -p 5189:5189 \

  -e AWS_ACCESS_KEY_ID=your-access-key \# Run the container

  -e AWS_SECRET_ACCESS_KEY=your-secret-key \docker run -d \

  -e AWS_REGION=me-central-1 \  -p 5102:5189 \

  -e ApiSettings__ApiKey=your-api-key \  -e AWS_ACCESS_KEY_ID=your-access-key \

  --name emailservice \  -e AWS_SECRET_ACCESS_KEY=your-secret-key \

  emailservice:latest  -e AWS_REGION=me-central-1 \

```  -e ApiSettings__ApiKey=your-api-key \

  --name emailservice \

---  emailservice:latest

```

## 🔧 Configuration

---

### Environment Variables

## 🔧 Configuration

The service requires the following environment variables:

### Environment Variables

| Variable | Description | Example |

|----------|-------------|---------|The service requires the following environment variables:

| `AWS_ACCESS_KEY_ID` | AWS Access Key | `AKIA...` |

| `AWS_SECRET_ACCESS_KEY` | AWS Secret Key | `secret...` || Variable | Description | Example |

| `AWS_REGION` | AWS Region | `me-central-1` ||----------|-------------|---------|

| `ApiSettings__ApiKey` | API Key for authentication | `your-secure-key` || `AWS_ACCESS_KEY_ID` | AWS Access Key | `AKIA...` |

| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | `Host=postgres;Port=5432;...` || `AWS_SECRET_ACCESS_KEY` | AWS Secret Key | `secret...` |

| `ASPNETCORE_ENVIRONMENT` | ASP.NET Environment | `Production` || `AWS_REGION` | AWS Region | `me-central-1` |

| `ApiSettings__ApiKey` | API Key for authentication | `your-secure-key` |

### Using .env File| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | `Host=postgres;Port=5432;...` |

| `ASPNETCORE_ENVIRONMENT` | ASP.NET Environment | `Production` |

Create or use `.env` in the EmailService root:

### Using .env File

```env

AWS_ACCESS_KEY_ID=AKIA46ALPORVS4J45A54Create or use `EmailService/.env`:

AWS_SECRET_ACCESS_KEY=evBK9EWyJeQxdnTbRMeaPUjGKtONInvC3tNcPdlV

AWS_REGION=me-central-1```env

API_KEY=dev-email-service-key-2024AWS_ACCESS_KEY_ID=AKIA46ALPORVS4J45A54

```AWS_SECRET_ACCESS_KEY=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

AWS_REGION=me-central-1

---API_KEY=dev-email-service-key-2024

```

## 📋 Docker Commands

---

### View Running Containers

```bash## 📋 Docker Commands

docker ps

```### View Running Containers

```bash

### View Logsdocker ps

```bash```

# Follow logs

docker logs -f emailservice### View Logs

```bash

# Last 100 lines# Follow logs

docker logs --tail 100 emailservicedocker logs -f emailservice

```

# Last 100 lines

### Stop Containerdocker logs --tail 100 emailservice

```bash```

docker stop emailservice

```### Stop Container

```bash

### Start Containerdocker stop emailservice

```bash```

docker start emailservice

```### Start Container

```bash

### Remove Containerdocker start emailservice

```bash```

docker rm -f emailservice

```### Remove Container

```bash

### Remove Imagedocker rm -f emailservice

```bash```

docker rmi emailservice:latest

```### Remove Image

```bash

---docker rmi emailservice:latest

```

## 🧪 Test the Dockerized Service

---

### Health Check (No API Key Required)

```bash## 🧪 Test the Dockerized Service

curl -X POST http://localhost:5189/graphql \

  -H "Content-Type: application/json" \```bash

  -d '{"query": "{ health { status service timestamp } }"}'# Health check

```curl http://localhost:5189/api/email/health



### Send Test Email via GraphQL# Send test email

```bashcurl -X POST http://localhost:5189/api/email/send \

curl -X POST http://localhost:5189/graphql \  -H "Content-Type: application/json" \

  -H "Content-Type: application/json" \  -H "X-API-Key: dev-email-service-key-2024" \

  -H "X-API-Key: dev-email-service-key-2024" \  -d '{

  -d '{    "to": "husain.f4l@gmail.com",

    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",    "subject": "Docker Test",

    "variables": {    "body": "Email from Dockerized EmailService",

      "input": {    "isHtml": false

        "to": "husain.f4l@gmail.com",  }'

        "subject": "Docker Test",```

        "body": "Email from Dockerized EmailService GraphQL API",

        "isHtml": false---

      }

    }## 🔄 Update and Rebuild

  }'

``````bash

# Stop and remove existing container

---docker-compose down



## 🔄 Update and Rebuild# Rebuild and start

docker-compose up -d --build

```bash

# Stop and remove existing container# Or manually

docker-compose downdocker stop emailservice

docker rm emailservice

# Rebuild and startdocker build -t emailservice:latest -f EmailService/Dockerfile .

docker-compose up -d --builddocker run -d -p 5189:5189 --env-file EmailService/.env --name emailservice emailservice:latest

```

# Or manually

docker stop emailservice---

docker rm emailservice

docker build -t emailservice:latest .## 📦 Production Deployment

docker run -d -p 5189:5189 --env-file .env --name emailservice emailservice:latest

```### Using Docker Compose (Recommended)



---1. Update `EmailService/.env` with production credentials

2. Update `appsettings.json` with production API key

## 📦 Production Deployment3. Run:



### Using Docker Compose (Recommended)```bash

docker-compose up -d

1. Update `.env` with production credentials```

2. Update `appsettings.json` with production API key

3. Run:### Push to Docker Registry



```bash```bash

docker-compose up -d# Tag the image

```docker tag emailservice:latest your-registry/emailservice:1.0.0



### Push to Docker Registry# Push to registry

docker push your-registry/emailservice:1.0.0

```bash```

# Tag the image

docker tag emailservice:latest your-registry/emailservice:1.0.0---



# Push to registry## 🐋 Docker Image Details

docker push your-registry/emailservice:1.0.0

```- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0`

- **Build Image**: `mcr.microsoft.com/dotnet/sdk:10.0`

---- **Exposed Port**: `5189`

- **Working Directory**: `/app`

## 🐋 Docker Image Details

---

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0`

- **Build Image**: `mcr.microsoft.com/dotnet/sdk:10.0`## 🔍 Troubleshooting

- **Exposed Port**: `5189`

- **Working Directory**: `/app`### Container not starting

- **API Type**: GraphQL

```bash

---# Check logs

docker logs emailservice

## 🔍 Troubleshooting

# Check if port is in use

### Container not startingsudo lsof -i :5189

```

```bash

# Check logs### Environment variables not loaded

docker logs emailservice

```bash

# Check if port is in use# Verify environment variables in container

sudo lsof -i :5189docker exec emailservice env | grep AWS

```docker exec emailservice env | grep ApiSettings

```

### Environment variables not loaded

### Permission issues

```bash

# Verify environment variables in container```bash

docker exec emailservice env | grep AWS# Run with appropriate permissions

docker exec emailservice env | grep ApiSettingsdocker-compose up -d --force-recreate

``````



### GraphQL endpoint not responding---



```bash## 📊 Monitoring

# Test health check

curl -X POST http://localhost:5189/graphql \### Check container status

  -H "Content-Type: application/json" \```bash

  -d '{"query": "{ health { status } }"}'docker ps -a

```

# Check if service is running

docker ps | grep emailservice### View resource usage

```bash

# View recent logsdocker stats emailservice

docker logs --tail 50 emailservice```

```

### Inspect container

### Permission issues```bash

docker inspect emailservice

```bash```

# Run with appropriate permissions

docker-compose up -d --force-recreate---

```

## ✅ Verification Checklist

---

- [ ] Docker image builds successfully

## 📊 Monitoring- [ ] Container starts without errors

- [ ] Health endpoint responds: `http://localhost:5189/api/email/health`

### Check container status- [ ] Email sending works with valid API key

```bash- [ ] Environment variables are loaded correctly

docker ps -a- [ ] Logs are accessible via `docker logs`

```

---

### View resource usage

```bash**Your EmailService is now containerized and ready for deployment!** 🐳

docker stats emailservice
```

### Inspect container
```bash
docker inspect emailservice
```

### Test GraphQL Playground (Development)
Open browser to: `http://localhost:5189/ui/playground`

---

## ✅ Verification Checklist

- [ ] Docker image builds successfully
- [ ] Container starts without errors
- [ ] GraphQL endpoint responds: `http://localhost:5189/graphql`
- [ ] Health query works without API key
- [ ] Email sending works with valid API key
- [ ] Environment variables are loaded correctly
- [ ] Logs are accessible via `docker logs`
- [ ] GraphQL Playground accessible in development

---

## 🌐 Accessing the Service

### Local Development
- **GraphQL Endpoint**: `http://localhost:5189/graphql`
- **GraphQL Playground**: `http://localhost:5189/ui/playground`

### Production
- Update firewall rules to allow port 5189
- Use reverse proxy (nginx/traefik) for HTTPS
- Set up proper DNS
- Use environment-specific API keys

---

## 🔐 Security Recommendations

1. **Use HTTPS in Production**: Always use a reverse proxy with SSL/TLS
2. **Secure API Keys**: Use secrets management (Docker secrets, HashiCorp Vault)
3. **Network Isolation**: Use Docker networks to isolate services
4. **Regular Updates**: Keep base images and dependencies updated
5. **Log Monitoring**: Set up centralized logging (ELK, Splunk, etc.)

---

**Your EmailService GraphQL API is now containerized and ready for deployment!** 🐳
