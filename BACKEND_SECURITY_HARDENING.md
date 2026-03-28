# Backend Security Hardening

## Scope

This document captures the current backend security baseline for `B09`.

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

## Dependency Posture

- `dotnet list package --vulnerable --include-transitive` was run for `FoodDiary.Web.Api` and `FoodDiary.Infrastructure`.
- No currently known vulnerable NuGet packages were reported from the configured sources during this pass.

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

## Remaining B09 Follow-Ups

- Review dependency versions and vulnerability posture as a dedicated pass.
- Add content-scanning or stricter post-upload validation if untrusted public image ingestion expands.
