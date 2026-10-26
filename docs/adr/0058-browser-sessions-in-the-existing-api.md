# 0058: Browser sessions in the existing API

Status: Accepted

Date: 2026-10-26

## Context

The user and admin Angular applications previously received access JWTs and kept
them in tab storage. Refresh JWTs used an HttpOnly cookie. This exposed reusable
access credentials to page JavaScript and duplicated token lifecycle logic in
both frontends. FoodDiary has one modular API and no external browser clients
requiring compatibility with the former login response.

## Decision

Implement opaque browser sessions within Identity and the existing API, without
adding a BFF service. Identity converts its internally issued refresh session
into a random browser credential and stores only its hash in the existing
session registry. Browser responses contain user, roles, public session ID and
remember-me metadata. Ordinary browser JavaScript receives neither JWT.

Use separate host-only HttpOnly cookies for the user application and admin SSO.
Use Secure and the `__Host-` prefix over HTTPS. Cookie requests require the
custom request header and trusted-origin checks described in
[Browser sessions](../backend/BROWSER_SESSIONS.md). Production admin requests
use the admin origin's API proxy.

Keep Telegram bot bearer authentication and the explicitly selected Telegram
Mini App token transport. Mini App access tokens stay in memory. Telegram
assertion validation, one-time tickets and browser binding retain their owners.

Exchange admin impersonation handoffs for a per-context HttpOnly cookie.
Store the protected impersonation JWT in the distributed cache. A public tab
selector chooses the context; existing actor, security version, originating
session and restricted-operation checks still determine its authority.

## Consequences

Computer and phone retain independent sessions, and logout revokes only the
selected session. Revocation and critical credential changes use the existing
registry. Session restoration can re-read current roles after a security-version
change but cannot restore a revoked, expired or disabled account session.

Cookie authentication introduces CSRF obligations. HttpOnly reduces credential
exfiltration through injected JavaScript; it does not prevent injected code from
acting through an already authenticated page. Authorization, XSS prevention and
provider proof validation remain necessary.

Deploy API, database migration, both frontend bundles and admin proxy together.
An old frontend expecting JWTs is incompatible. Rollback requires signing in
again; converted browser records cannot be exchanged for legacy JWTs.

The API compatibility acceptance names the two nullable `accessToken` DTO
transitions and binds them to exact OpenAPI/payload snapshot pairs and the
successful ancestral CI baseline. It grants no unrelated compatibility waiver.
