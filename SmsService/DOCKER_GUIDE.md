# Docker Deployment Guide

## Quick Start

### Option 1: With Local PostgreSQL (Development)
```bash
# Build and start all services (SMS + PostgreSQL)
docker-compose up -d

# View logs
docker-compose logs -f sms-service

# Stop services
docker-compose down
```

### Option 2: With External PostgreSQL (Production)
```bash
# Build and start SMS service only
docker-compose -f docker-compose.prod.yml up -d

# View logs
docker-compose -f docker-compose.prod.yml logs -f

# Stop service
docker-compose -f docker-compose.prod.yml down
```

## Build Docker Image Only

```bash
# Build the image
docker build -t sms-service:latest .

# Run the container
docker run -d \
  -p 5103:5103 \
  -e ConnectionStrings__DefaultConnection="Host=149.200.251.12;Port=5432;Database=aqlaansms;Username=husain;Password=tt55oo77" \
  -e JosmsSettings__BaseUrl="https://www.josms.net" \
  -e JosmsSettings__AccName="margogroup" \
  -e JosmsSettings__AccPassword="nR@9g@Z7yV0@sS9bX1y" \
  -e JosmsSettings__DefaultSenderId="MargoGroup" \
  -e ApiSettings__ApiKey="sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=" \
  --name sms-service \
  sms-service:latest
```

## Environment Variables

### Required
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `JosmsSettings__AccName` - JOSMS account name
- `JosmsSettings__AccPassword` - JOSMS account password
- `ApiSettings__ApiKey` - API key for authentication

### Optional
- `JosmsSettings__BaseUrl` - JOSMS gateway URL (default: https://www.josms.net)
- `JosmsSettings__DefaultSenderId` - Default sender ID (default: MargoGroup)
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `ASPNETCORE_URLS` - Listening URLs (default: http://+:5103)

## Testing the Service

```bash
# Health check
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health }"}'

# Send SMS
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sms-prod-o02az0sVxwe2YtKhRXoB+XFB9u4FxGU2h8zO/sjU+9I=" \
  -d '{
    "query": "mutation { sendGeneral(input: { to: \"962771122003\", message: \"Test from Docker\" }) { success message } }"
  }'
```

## Docker Compose Files

### `docker-compose.yml` (Development)
- Includes PostgreSQL container
- Database data persisted in Docker volume
- Automatic database initialization
- Suitable for local development and testing

### `docker-compose.prod.yml` (Production)
- SMS service only
- Connects to external PostgreSQL (149.200.251.12)
- Production-ready configuration
- Suitable for deployment

## Health Checks

The service includes built-in health checks:
- **Endpoint:** `POST /graphql` with query `{ health }`
- **Interval:** Every 30 seconds
- **Timeout:** 10 seconds
- **Retries:** 3
- **Start Period:** 40 seconds

View health status:
```bash
docker ps
# Look for "healthy" in STATUS column
```

## Networking

### Development (docker-compose.yml)
- Network: `sms-network` (bridge)
- SMS Service: `sms-service` (accessible at port 5103)
- PostgreSQL: `postgres` (accessible at port 5432)
- Services communicate via container names

### Production (docker-compose.prod.yml)
- Network: `sms-network` (bridge)
- SMS Service: `sms-service` (accessible at port 5103)
- Database: External (149.200.251.12:5432)

## Volumes

### Development
- `postgres-data`: PostgreSQL data persistence

### Production
- No volumes (uses external database)

## Logs

```bash
# View all logs
docker-compose logs

# Follow logs
docker-compose logs -f sms-service

# Last 100 lines
docker-compose logs --tail=100 sms-service

# Logs for specific service
docker logs sms-service
```

## Database Management

### Development (with local PostgreSQL)
```bash
# Access PostgreSQL container
docker exec -it sms-postgres psql -U husain -d aqlaansms

# Run SQL queries
\dt  # List tables
SELECT COUNT(*) FROM sms_messages;

# Backup database
docker exec sms-postgres pg_dump -U husain aqlaansms > backup.sql

# Restore database
cat backup.sql | docker exec -i sms-postgres psql -U husain -d aqlaansms
```

### Production (external database)
Use standard PostgreSQL tools to connect to 149.200.251.12:
```bash
psql -h 149.200.251.12 -U husain -d aqlaansms
```

## Troubleshooting

### Service won't start
```bash
# Check logs
docker-compose logs sms-service

# Check if port is already in use
lsof -i :5103

# Rebuild image
docker-compose build --no-cache
docker-compose up -d
```

### Database connection issues
```bash
# Test database connection from container
docker exec sms-service pg_isready -h postgres -U husain

# For production (external DB)
docker exec sms-service pg_isready -h 149.200.251.12 -U husain
```

### Container keeps restarting
```bash
# Check health status
docker inspect sms-service | grep -A 10 Health

# Check environment variables
docker exec sms-service env | grep -E "ConnectionStrings|Josms|Api"

# View detailed logs
docker logs sms-service --timestamps
```

## Updating the Service

```bash
# Pull latest changes
git pull

# Rebuild and restart
docker-compose down
docker-compose build --no-cache
docker-compose up -d

# Verify
docker-compose ps
docker-compose logs -f sms-service
```

## Production Deployment

### Using Docker Compose
```bash
# 1. Copy files to server
scp docker-compose.prod.yml user@server:/opt/sms-service/
scp Dockerfile user@server:/opt/sms-service/

# 2. SSH to server
ssh user@server

# 3. Navigate to directory
cd /opt/sms-service

# 4. Start service
docker-compose -f docker-compose.prod.yml up -d

# 5. Verify
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health }"}'
```

### Using Docker Swarm
```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.prod.yml sms-stack

# List services
docker service ls

# Scale service
docker service scale sms-stack_sms-service=3

# View logs
docker service logs sms-stack_sms-service
```

### Using Kubernetes
```bash
# Build and push to registry
docker build -t your-registry/sms-service:1.0.0 .
docker push your-registry/sms-service:1.0.0

# Create deployment
kubectl apply -f k8s-deployment.yaml

# Expose service
kubectl expose deployment sms-service --port=5103 --type=LoadBalancer
```

## Security Considerations

1. **Environment Variables:** Never commit secrets to git
2. **API Keys:** Rotate regularly
3. **Database Password:** Use strong passwords
4. **Network:** Use firewall rules to restrict access
5. **HTTPS:** Use reverse proxy (nginx/traefik) for SSL termination

## Monitoring

### Basic Monitoring
```bash
# Resource usage
docker stats sms-service

# Health checks
watch -n 5 'docker inspect sms-service | grep -A 5 Health'
```

### With Prometheus
Add Prometheus scraping configuration to monitor metrics endpoint.

### With Grafana
Create dashboards to visualize:
- SMS sending rate
- Success/failure ratio
- Response times
- Database connection pool

## Backup Strategy

### Daily Backups
```bash
# Create backup script
cat > backup.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec sms-postgres pg_dump -U husain aqlaansms > backup_$DATE.sql
# Keep last 7 days
find . -name "backup_*.sql" -mtime +7 -delete
EOF

chmod +x backup.sh

# Add to crontab
crontab -e
# Add: 0 2 * * * /path/to/backup.sh
```

## Performance Tuning

### Increase Resources
```yaml
services:
  sms-service:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 512M
```

### Connection Pooling
Adjust PostgreSQL connection pool in appsettings.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Pooling=true;Min Pool Size=5;Max Pool Size=100"
  }
}
```

## Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Verify environment variables
3. Test database connectivity
4. Review GraphQL schema: `POST /graphql` with introspection query

---

**Last Updated:** November 17, 2025  
**Docker Image:** .NET 10.0  
**Service Port:** 5103  
**GraphQL Endpoint:** http://localhost:5103/graphql
