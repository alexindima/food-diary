# FoodDiary Threat Model

## Overview

FoodDiary is a multi-client personal health and nutrition platform. Its primary
runtime is an ASP.NET Core API and Angular client, with separate job manager,
Telegram bot, mail relay, mail inbox, PostgreSQL stores, Redis, RabbitMQ, object
storage, payment, identity, AI, and wearable-provider integrations.

The highest-value assets are account credentials and sessions, health and
nutrition history, body measurements, private content, billing state, admin
privileges, provider credentials, webhook authenticity, and integrity of
cross-service messages and background jobs.

## Threat Model, Trust Boundaries, and Assumptions

Trust boundaries:

- Browser/mobile/Telegram clients to the public API: request data, headers,
  uploaded content, tokens, IDs, and callback parameters are attacker-controlled
  until validated and authorized.
- Public API to application/domain code: presentation authentication does not
  replace per-use-case ownership and role checks.
- Application to databases, cache, broker, storage, mail, payment, AI, identity,
  USDA, and wearable providers: remote responses, callbacks, retry timing, and
  availability are not inherently trusted.
- Primary application to MailRelay and MailInbox: access is allowed only through
  approved client packages and integration adapters.
- Webhooks and bot endpoints to internal commands: signatures, secrets,
  timestamps, replay/idempotency keys, and payload bounds must be verified before
  side effects.
- Admin interfaces to privileged operations: authentication alone is
  insufficient; current role and resource scope must be checked server-side.
- CI, migrations, initializers, and deployment configuration to production are
  operator/developer-controlled privileged inputs.

Assumptions:

- TLS is terminated by trusted infrastructure and forwarded headers are accepted
  only from configured proxies.
- Production secrets are supplied outside source control, scoped, and rotated.
- Database and broker administration are outside the ordinary attacker model.
- Email-account or end-user-device compromise is not preventable here, but token
  lifetime, rotation, and revocation should limit impact.

Security invariants:

- Users access only resources they own unless a documented collaboration or admin
  policy grants access.
- Admin checks use current server-side state, not only stale token claims.
- Refresh, reset, verification, SSO, webhook, and idempotency tokens cannot be
  replayed beyond their intended lifecycle.
- Money and subscription state comes from authenticated, idempotently processed
  provider events, never client assertions.
- Logs, metrics, traces, errors, and analytics exclude credentials, raw tokens,
  provider secrets, unnecessary health data, and private message bodies.
- Supporting services cannot become a path around the primary service boundary.

## Attack Surface, Mitigations, and Attacker Stories

Primary surfaces include public/admin controllers, SignalR, auth and recovery,
Telegram authentication, uploads, billing and webhooks, mail, AI inputs,
wearable callbacks, background jobs, and personal-data exports.

Relevant attacker stories include account enumeration/takeover, cross-user ID
substitution, replay of privileged messages, SSRF/XSS/injection through remote
content, resource exhaustion, forged billing state, and exfiltration through
telemetry or third parties.

Existing controls include strict JWT validation, hashed and rotated refresh
tokens, sensitive-operation rate limits, allowlist CORS, trusted-proxy handling,
constant-time bot-secret comparison, single-use SSO codes, hashed recovery
tokens, upload restrictions, architecture tests, and
`docs/backend/BACKEND_SECURITY_HARDENING.md`.

Attacks requiring database, cloud-account, CI-runner, or end-user-device
administrative control are out of scope unless the code unnecessarily amplifies
that access or omits an expected boundary.

## Severity Calibration (Critical, High, Medium, Low)

- Critical: unauthenticated code execution, production signing-key extraction,
  systemic authentication bypass, or mass health/payment compromise.
- High: account takeover, admin escalation, substantial cross-user disclosure,
  forged durable billing changes, or unrestricted privileged-network SSRF.
- Medium: bounded IDOR, replay causing duplicate work, persistent XSS requiring
  interaction, sensitive metadata leakage, or practical resource-limit bypass.
- Low: low-sensitivity disclosure, defense-in-depth gaps without a realistic
  attack path, or minor availability impact.

Repository: food-diary
Version: 4bfe50dd536e5907866b4a66563547fc84365c24
