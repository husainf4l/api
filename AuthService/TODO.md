# AuthService Hardening To‑Do

- [x] Scrub all documentation and configs for leaked credentials and rotate anything exposed.
- [x] Replace `AllowAll` CORS policy with per-environment allowlists backed by the `CorsOrigins` table.
- [x] Enforce HTTPS everywhere (`RequireHttpsMetadata`, HSTS, TLS termination).
- [x] Hash refresh tokens at rest, log issuance metadata, and detect reuse events.
- [x] Add rate limiting, per-account lockout, and suspicious-IP alerts to login/refresh endpoints.
- [x] Expand password policy (strength checks, breach detection, fixed-time comparisons, no reuse). 
- [ ] Implement email verification, MFA (TOTP/U2F), and password reset flows with secure tokens.
- [ ] Configure production values for `Security:Cleanup:IntervalMinutes` & `Security:DormantAccountDays`, tied to secrets manager per environment.
- [ ] Extend dormant account job with notifications and optional auto-deactivation workflow.
- [x] Instrument audit logging for login/refresh/revoke/validate, ship logs to SIEM, and add monitoring/alerting dashboards.
- [x] Automate expired refresh-token cleanup and other background security jobs (e.g., dormant-account review).
- [x] Document secure deployment runbooks, incident-response playbooks, and recovery procedures with on-call contacts.