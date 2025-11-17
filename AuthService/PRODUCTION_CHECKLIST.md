# AuthService Production Checklist

## Pre-Deployment Checklist

### Security
- [ ] Change JWT Secret in appsettings.json to a strong random value (min 32 characters)
- [ ] Update database credentials in .env
- [ ] Rotate any credentials that were previously shared publicly before deployment
- [ ] Enable HTTPS only (disable HTTP in production)
- [ ] Update CORS policy (replace AllowAll with specific origins)
- [ ] Ensure .env is not committed to version control
- [ ] Set secure password requirements
- [ ] Enable rate limiting on authentication endpoints
- [ ] Implement account lockout after failed login attempts

### Configuration
- [ ] Set appropriate token expiry times for production
  - Recommended: Access token = 15-30 minutes
  - Recommended: Refresh token = 7-30 days
- [ ] Configure proper logging levels
- [ ] Set up error tracking (e.g., Sentry, Application Insights)
- [ ] Configure connection pooling for database
- [ ] Set up health checks endpoint monitoring
- [ ] Configure proper ASPNETCORE_ENVIRONMENT variable

### Database
- [ ] Ensure PostgreSQL is properly secured
- [ ] Create database backup strategy
- [ ] Set up database monitoring
- [ ] Create database indexes for performance
  - Users.Email (already unique)
  - RefreshTokens.Token (already unique)
  - RefreshTokens.UserId + IsRevoked + ExpiresAt
- [ ] Plan for database migrations in production
- [ ] Set up automated expired token cleanup job

### Infrastructure
- [ ] Set up load balancer (if scaling horizontally)
- [ ] Configure SSL/TLS certificates
- [ ] Set up reverse proxy (Nginx, Apache, or cloud LB)
- [ ] Configure firewall rules
- [ ] Set up monitoring and alerting
- [ ] Configure automatic backups
- [ ] Set up CI/CD pipeline

### Testing
- [ ] Test registration flow
- [ ] Test login flow
- [ ] Test token refresh flow
- [ ] Test token revocation
- [ ] Test token validation
- [ ] Test with expired tokens
- [ ] Test with invalid tokens
- [ ] Test concurrent requests
- [ ] Load test authentication endpoints
- [ ] Test database connection failure scenarios

### Documentation
- [ ] Document API endpoints for other teams
- [ ] Document integration process
- [ ] Create runbook for common issues
- [ ] Document deployment process
- [ ] Document rollback procedure

### Monitoring
- [ ] Set up application performance monitoring (APM)
- [ ] Configure alerts for:
  - High error rates
  - Failed login attempts
  - Database connection issues
  - High response times
  - Token generation failures
- [ ] Set up log aggregation
- [ ] Create dashboards for key metrics

## Post-Deployment Checklist

### Immediate (First 24 hours)
- [ ] Monitor error logs
- [ ] Check authentication success rate
- [ ] Verify database connections are stable
- [ ] Test all endpoints in production
- [ ] Verify CORS is working for all client apps
- [ ] Check token generation and validation
- [ ] Monitor response times

### First Week
- [ ] Review security logs
- [ ] Analyze failed login patterns
- [ ] Check for any performance issues
- [ ] Verify backup systems are working
- [ ] Review and optimize database queries
- [ ] Check token refresh patterns

### Ongoing
- [ ] Regular security audits
- [ ] Keep dependencies updated
- [ ] Monitor token usage patterns
- [ ] Review and update CORS policies
- [ ] Optimize database performance
- [ ] Regular backup testing

## Recommended Enhancements

### High Priority
- [ ] Email verification
- [ ] Password reset functionality
- [ ] Rate limiting
- [ ] Account lockout mechanism
- [ ] IP-based access control

### Medium Priority
- [ ] Two-factor authentication (2FA)
- [ ] OAuth provider integration (Google, Facebook)
- [ ] Password strength requirements
- [ ] Session management
- [ ] Audit logging

### Nice to Have
- [ ] Role-based access control (RBAC)
- [ ] API key authentication (for service-to-service)
- [ ] Biometric authentication support
- [ ] Single Sign-On (SSO)
- [ ] Social media login

## Environment Variables (Production)

Ensure these are set in your production environment:

```bash
# Required
DATABASE_HOST=your-production-db-host
DATABASE_PORT=5432
DATABASE_USER=your-production-db-user
DATABASE_PASSWORD=your-strong-db-password
DATABASE_NAME=your-production-db-name

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Optional but recommended
LOG_LEVEL=Information
ENABLE_DETAILED_ERRORS=false
```

## appsettings.Production.json

Create this file with production-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "Jwt": {
    "Secret": "YOUR-PRODUCTION-SECRET-MINIMUM-32-CHARACTERS-LONG",
    "Issuer": "AuthService",
    "Audience": "AuthServiceClients",
    "AccessTokenExpiryMinutes": "30",
    "RefreshTokenExpiryDays": "7"
  },
  "AllowedHosts": "your-domain.com"
}
```

## Common Issues and Solutions

### Issue: Database connection fails
**Solution**: Check .env file, verify PostgreSQL is running, check firewall rules

### Issue: JWT validation fails
**Solution**: Ensure JWT Secret matches between token generation and validation

### Issue: CORS errors
**Solution**: Update CORS policy to include your client domains

### Issue: Tokens expire too quickly
**Solution**: Adjust AccessTokenExpiryMinutes in appsettings.json

### Issue: High memory usage
**Solution**: Review DbContext lifecycle, ensure proper disposal

## Support Contacts

- **Technical Lead**: [Your Name]
- **DevOps**: [Team Contact]
- **Database Admin**: [DBA Contact]
- **Security Team**: [Security Contact]

## Rollback Plan

If issues occur:

1. Switch traffic back to old service (if applicable)
2. Rollback database migrations: `./migration.sh rollback`
3. Deploy previous version
4. Notify all teams
5. Investigate and document the issue
6. Create hotfix if needed

---

**Last Updated**: [Date]  
**Service Version**: 1.0.0  
**Reviewed By**: [Name]
