# AuthService Secure Deployment & Incident Runbook

## 1. Pre-Deployment Checklist
- Validate environment variables (JWT secret, DB creds, SIEM endpoint, notification service).
- Ensure migrations are up to date: `dotnet ef database update`.
- Verify CORS origins and OAuth clients (if applicable) are configured for the target environment.
- Confirm secrets are stored in Vault/KeyVault/Parameter Store; never in git.
- Review monitoring/alerting dashboards (error rate, login failures, SIEM feed) before rollout.
- Announce maintenance window and notify dependent teams (minimum 24h notice for prod).

## 2. Deployment Steps
1. Tag release in git (`vX.Y.Z`), build artifacts via CI.
2. Apply infra changes (Terraform/ARM/etc.) if needed.
3. Deploy container or app service:
   ```
   docker compose pull authservice
   docker compose up -d authservice
   ```
4. Run smoke tests:
   - `POST /api/auth/register` (test account)
   - `POST /api/auth/login`
   - `POST /api/auth/refresh`
   - `POST /api/auth/validate`
   - `GET /health`
5. Remove test accounts/tokens.
6. Announce completion and update status pages.

## 3. Post-Deployment Verification
- Monitor SIEM/metrics for 30 minutes (errors, latency, login failure spikes).
- Check background job logs (refresh token cleanup, dormant account review).
- Validate alerts (rate limit, lockout) fire in lower environment before prod.
- Confirm Swagger disabled in prod (if required) and HTTPS enforced.

## 4. Incident Response Playbook
### P1 – Authentication Outage
1. Declare incident in incident tool, page on-call AuthService + SRE.
2. Triage: check `dotnet logs`, DB connectivity, external dependencies (SMTP, SIEM).
3. Mitigate:
   - Roll back to last working container (`docker compose rollback` or redeploy previous tag).
   - If DB migration caused issue, run rollback script `./migration.sh rollback`.
4. Communicate status every 15 minutes to stakeholders.
5. Post-incident: root-cause analysis within 24h, update runbook if new failure mode identified.

### P2 – Security Event (Token Theft, Brute Force)
1. Engage Security on-call immediately.
2. Revoke compromised refresh tokens via admin tooling/SQL.
3. Enable heightened rate limiting if attack in progress.
4. Coordinate password reset/email notifications as needed.

## 5. Recovery Procedures
- **Config rollback**: restore previous `.env`/configuration from secrets manager versions.
- **Database restore**: trigger point-in-time restore, update connection strings, run integrity checks.
- **Token revocation sweep**: execute `IRefreshTokenRepository.RevokeAllUserTokensAsync` for affected users.
- **Dormant account cleanup**: adjust `Security:DormantAccountDays` and restart service.

## 6. Contacts & Escalation
- AuthService On-call: `oncall-auth@example.com`
- SRE/Platform: `sre-oncall@example.com`
- Security Operations: `soc@example.com`
- Product Owner: `product-auth@example.com`

Keep this document in sync with operational changes. Review quarterly with Security + SRE.

