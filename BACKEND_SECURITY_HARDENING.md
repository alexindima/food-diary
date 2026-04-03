# Backend Security Hardening

## Scope

This document captures the current backend security baseline.

## Current Hardening Baseline

- Sensitive auth endpoints use the auth rate-limit policy:
  - register
  - login
  - refresh
  - restore
  - verify-email
  - verify-email resend
  - password-reset request
  - password-reset confirm
  - admin SSO exchange
  - Telegram auth verify/login-widget/bot-auth
- Image upload URL generation uses the auth rate-limit policy because it is abuse-prone and creates external storage side effects.
- Telegram bot authentication requires `X-Telegram-Bot-Secret` and compares it in constant time.
- Admin SSO start remains admin-only and exchange uses single-use cached codes with a short TTL.
- JWT validation uses issuer, audience, signing key, lifetime validation, and zero clock skew.
- Repository CORS is allowlist-based; wildcard origins are not used with credentials enabled.
- HTTP logging avoids query-string logging, which keeps hub access tokens out of host logs.

## Rate Limiting Trust Boundary

- Rate-limiter partitioning trusts authenticated user IDs first.
- For anonymous callers, the limiter uses `RemoteIpAddress`.
- Raw `X-Forwarded-For` is not trusted directly for rate limiting, because client-controlled forwarded headers can be used to evade throttling if proxy trust is not configured explicitly.
- Trusted reverse proxies and networks must be configured through `ForwardedHeaders` host options before forwarded client IPs are honored.
- The backend test suite now includes smoke coverage for both sides of this boundary:
  - spoofed forwarded headers are ignored by rate-limit partitioning
  - forwarded client IP/proto are only applied when the immediate proxy is explicitly trusted

## Dependency Posture

- Backend CI now runs `dotnet list package --vulnerable --include-transitive` for:
  - `FoodDiary.Web.Api`
  - `FoodDiary.Infrastructure`
  - `FoodDiary.Application`
- Local verification on the current baseline reports no known vulnerable NuGet packages from the configured sources.

## Upload Flow Expectations

- Presigned uploads only allow a small image MIME allowlist.
- Max upload size is enforced by `S3:MaxUploadSizeBytes`.
- Upload URLs are short-lived.
- Image assets are tracked before use so later delete/reference rules can be enforced.

## Focused Threat Model

### JWT And Refresh Tokens

- Primary risks:
  - signing-key disclosure
  - replay of stolen refresh tokens
  - accepting expired or cross-environment tokens
- Current mitigations:
  - access and refresh tokens require issuer, audience, signing key, and lifetime validation
  - clock skew is zero
  - refresh tokens are stored hashed in persistence
  - refresh requests rotate the stored refresh token on successful use
  - user deletion clears the stored refresh token
- Residual risk:
  - a stolen refresh token is still usable until the first successful legitimate rotation, expiry, or explicit server-side invalidation

### Admin SSO

- Primary risks:
  - non-admin token exchange
  - replay of SSO codes
  - code leakage through logs or browser history
- Current mitigations:
  - start endpoint is admin-only
  - exchange re-checks admin role against the current user record
  - codes are high-entropy, single-use, and short-lived in distributed cache
  - HTTP logging does not include query strings
- Residual risk:
  - any external handoff or frontend flow using the code still needs to avoid client-side persistence, analytics capture, or referrer leakage

### Password Reset And Email Verification

- Primary risks:
  - account enumeration
  - replay of leaked reset or verification tokens
  - mail-delivery failures leaking account state
- Current mitigations:
  - reset and verification tokens are stored hashed
  - reset request handler avoids revealing whether an account exists
  - reset and resend flows have cooldown/rate-limit protection
- Residual risk:
  - mailbox compromise remains out of backend control and should be treated as full compromise of email-based recovery flows

### Image Upload Flow

- Primary risks:
  - storage abuse through bulk presign generation
  - upload of unsupported or oversized files
  - orphaned assets and unsafe references
- Current mitigations:
  - upload-url generation is authenticated and rate-limited
  - MIME allowlist and size limits are enforced before presign
  - presigned URLs expire quickly
  - assets are persisted so delete/reference rules can be enforced later
- Residual risk:
  - MIME validation is metadata-based at presign time; deep content inspection is not part of the current backend path

## Secret Rotation Expectations

### JWT Signing Secret

Rotation trigger:

- suspected disclosure
- environment compromise
- scheduled credential refresh

Rotation steps:

1. Generate a new secret with at least the current required length.
2. Update the secret in the deployment secret store for the target environment.
3. Redeploy the API host so new tokens are signed with the new key.
4. Expect all previously issued access and refresh tokens for that environment to become invalid immediately.
5. Confirm login and refresh flows succeed with newly issued tokens after deploy.

Operational note:

- this is a hard cutover, not multi-key validation; plan for forced re-authentication.

### Telegram Bot Secret

Rotation steps:

1. Generate a new secret.
2. Update the bot-side sender and `TelegramBot:ApiSecret` in the target environment.
3. Deploy both sides in a coordinated window.
4. Verify `/api/auth/telegram/bot/auth` succeeds with the new header and fails with the old one.

Operational note:

- rotate bot sender and API receiver together to avoid a transient auth outage.

### SMTP Credentials

Rotation steps:

1. Issue new SMTP credentials from the mail provider.
2. Update `Email:SmtpUser` and `Email:SmtpPassword` in the secret store.
3. Redeploy the backend.
4. Validate both email verification and password-reset delivery.

Operational note:

- after rotation, confirm no provider-side app-password or tenant policy still points at the revoked credential.

### S3 Credentials

Rotation steps:

1. Create new access credentials with the same minimum required bucket permissions.
2. Update `S3:AccessKeyId` and `S3:SecretAccessKey` in the secret store.
3. Redeploy the backend.
4. Validate upload-url generation and asset delete flow.
5. Revoke the old credentials after successful verification.

Operational note:

- avoid deleting the old key before the new deployment is verified, or upload/delete flows may fail mid-rollout.

## Remaining Follow-Ups

- Keep dependency audit green in CI and review any new advisory before merge.
- Add content-scanning or stricter post-upload validation if untrusted public image ingestion expands.

## Recurring Security Review Checklist

Use this checklist for:

- notable auth/admin changes
- deploys that touch secrets, proxying, or storage
- release candidates
- quarterly backend security review

Mark each item as `ok`, `risk accepted`, or `follow-up`.

### 1. Authentication And Session Flows

- JWT settings are still strict:
  - issuer validation enabled
  - audience validation enabled
  - signing key validation enabled
  - lifetime validation enabled
  - zero clock skew
- refresh-token rotation still happens on successful refresh
- refresh tokens remain hashed at rest
- deleted and inactive users are blocked consistently in:
  - login
  - refresh
  - password reset request
  - Telegram auth flows
  - admin SSO exchange
- restore flow remains the only supported path from deleted back to active
- email verification and password reset tokens remain hashed at rest
- auth endpoints still use the auth rate-limit policy

### 2. Admin Surface

- admin-only controllers still require explicit admin authorization
- admin SSO start is still admin-only
- admin SSO exchange still re-checks current admin membership against persisted user data
- admin user updates still avoid deleted-user reactivation via normal active toggle
- admin-sensitive logs do not leak secrets, tokens, or codes
- no new admin endpoint bypasses audit logging expectations

### 3. Upload And Asset Flows

- upload-url generation remains authenticated and rate-limited
- MIME allowlist is still narrow and intentional
- size limits are still enforced before presign
- presigned URL TTL remains short
- asset tracking still happens before external use/reference
- delete flows still respect in-use protection
- any new public asset exposure is reviewed for enumeration and orphan risks

### 4. Telegram And External Adapters

- Telegram bot callback auth still requires `X-Telegram-Bot-Secret`
- secret comparison remains constant-time
- Telegram init/login validation still checks integrity before trust
- bot and API secrets are rotated together when changed
- no new Telegram endpoint bypasses rate limiting or current-user lifecycle checks

### 5. Proxy, Network, And Request Trust

- forwarded headers are only trusted through configured known proxies/networks
- rate limiting still ignores spoofed raw forwarded headers
- CORS remains allowlist-based
- credentials are not combined with wildcard origins
- query-string logging is still avoided for sensitive transport paths

### 6. Data Mutation Safety

- idempotency protection still covers the write endpoints that need replay defense
- deleted-user cleanup and reassignment rules still target only active, non-deleted users
- lifecycle checks still cover current-user CRUD and tracking flows
- no new handler silently relies on repository filtering where explicit lifecycle policy is required

### 7. Secrets And Deployment

- `/etc/fooddiary/fooddiary.env` remains the canonical server env source
- deploy still syncs compose from repo before running compose commands
- compose runtime still uses internal service hostnames for container-to-container connections
- JWT, SMTP, S3, Telegram, and OpenAI secrets are not committed to repo config
- secret rotation steps remain valid after deploy workflow changes

### 8. Dependency And Package Posture

- `dotnet list package --vulnerable --include-transitive` remains green for:
  - `FoodDiary.Web.Api`
  - `FoodDiary.Infrastructure`
  - `FoodDiary.Application`
- new security-relevant packages are reviewed before adoption
- any major-version auth/storage package change gets a focused regression review

## Security Review Cadence

### Every security-relevant PR

- run the checklist sections touched by the change
- note any intentional risk acceptance in the PR description

### Before release or staging promotion

- run the full checklist
- verify:
  - auth flows
  - admin flows
  - upload-url flow
  - Telegram auth flow
  - post-deploy readiness checks

### Quarterly

- re-read this document against current code
- review residual risks
- remove stale assumptions
- add new checklist items for newly introduced subsystems
