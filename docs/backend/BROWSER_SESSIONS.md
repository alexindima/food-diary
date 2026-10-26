# Browser sessions

The user website and admin UI authenticate through opaque HttpOnly cookies in
the existing API (ADR 0058). Telegram's server bot retains bearer authentication.

## Sign-in and restoration

1. The existing password, Google, account restore, password reset or browser
   Telegram flow verifies the user's proof and issues an internal Identity
   authentication result.
2. The HTTP result filter converts the issued refresh-session record into a
   browser session. Its credential is a session GUID plus a random 256-bit
   secret. Only its SHA-256 hash is stored; the credential is sent in Set-Cookie.
   Replacing a cookie revokes its previous proven session, preserving other devices.
3. The JSON response contains `user`, `roles`, `sessionId` and `rememberMe`.
   `accessToken` and `refreshToken` are absent for ordinary browsers.
4. On reload, `POST /api/v1/auth/session` proves the cookie and returns fresh
   metadata. The frontend does not trust stored role or identity hints as proof.
   A changed session or user invalidates a running tab rather than rebinding it.

Each authenticated request verifies the secret, active registry record, expiry,
current account and security version. Restoration may synchronize a changed
security version after reading the current principal. Critical operations that
revoke sessions cannot be bypassed by restoration.

The existing server maximum lifetime is 30 days, or 90 days with remember-me.
Remember-me also makes the cookie persistent; otherwise it is a browser session
cookie (browser session restoration settings can preserve such cookies).
Restoration atomically records activity without rotating the proof or sliding the
maximum lifetime; concurrent tab restoration does not compete on a tracked row.
Computer, phone and separate
browser profiles get separate records. Tabs in one browser share the ordinary
cookie. Logout revokes the selected record; the session-management endpoints can
revoke another device or all other sessions.

## HTTP boundary

- HTTP development uses `fooddiary.browser` and `fooddiary.admin`. HTTPS uses
  `__Host-fooddiary.browser` and `__Host-fooddiary.admin`: Secure, HttpOnly,
  Path `/`, no Domain, SameSite Lax. Trusted proxy headers must identify HTTPS.
- `X-FoodDiary-Client` selects `web`, `admin` or explicit `telegram` transport.
  Admin API paths always select the admin cookie.
- Unsafe browser/auth-entry requests require `X-Requested-With: XMLHttpRequest`.
  A supplied Origin must match the API origin or an exact configured CORS origin.
  Production admin cookie requests require the same origin. This depends on
  keeping credentialed CORS restricted to trusted application origins.
- `X-FoodDiary-Session` is a public expected-session GUID. A mismatch is rejected,
  including on anonymous logout, before a stale tab can affect a replacement
  session. It is not an authentication credential.
- Explicit bearer and impersonation selectors never fall back to ordinary cookie
  authentication. External presigned storage requests do not acquire app headers.
- Browser login/logout operations remain serialized through the frontend cookie
  queue and Web Locks where supported. The server enforces expected-session
  checks independently of cross-tab invalidation hints.

Normal browser credentials cannot mint JWTs through `/auth/refresh`. Conversion
replaces the stored refresh proof and refresh handling rejects browser records.
Legacy stored browser tokens are discarded on startup.

## Admin and impersonation

Admin SSO issues its own opaque cookie and leaves the main site's cookie intact.
Its server expiry is capped by the original SSO access ticket lifetime; the
ordinary 30/90-day browser-session lifetime does not extend administrator access.
The production admin Nginx host proxies `/api/`; the admin SDK uses that origin.

An impersonation exchange returns only `contextId`. Its random credential lives
in a separate HttpOnly cookie per context. The JWT is protected with ASP.NET Data
Protection and stored in `IDistributedCache` under the context and credential
hash. API replicas need shared cache and Data Protection keys. Missing cache,
expiry or an invalid context fails closed. `sessionStorage` holds only the public
selector; `X-FoodDiary-Context` and the hub `fd_context` query choose that context.
Two support tabs can target different users without replacing the main account.
Logout removes only the selected impersonation context. Existing Admin actor,
originating-session and security-version checks still apply to every request.

SignalR negotiates with cookies and an optional context selector. Authentication
expiry is bounded by the configured access-token connection lifetime, registry
expiry and account access cap; reconnect authenticates again. Existing realtime
connection and notification reconciliation behavior is retained.

## Telegram

Bot-secret authentication still returns a bearer JWT for server operations.
Explicit Mini App authentication requests with `X-FoodDiary-Client: telegram`
retain the existing refresh-cookie protocol and return an access JWT held only
in frontend memory. This includes fresh password/provider proofs used inside the
Mini App. Protected requests in this mode never fall back to the website cookie
when their bearer is missing. Browser Telegram OIDC without that mode produces an
ordinary browser session. A client header alone never replaces Telegram proof.

Mini App browser binding and third-party cookie requirements are unchanged.
Local integration tests cannot establish Telegram Web, iOS or Android cookie
compatibility; those device flows require the Telegram test stand.

## Release and rollback

Apply `20261025190554_AddBrowserSessions` before enabling the new API. It adds a
nullable `BrowserSecurityVersion` to the existing session table; it does not
rewrite existing sessions or create another store. Publish matching API and
user/admin bundles plus the admin API proxy. Check login, reload, independent
devices, scoped logout, admin SSO, isolated impersonation and bot authentication.

Do not log cookies, credentials, cached JWTs or authentication request bodies.
The new mediator request models redact credentials from JSON and ToString.
Public selectors and response metadata must not be treated as proof.

Rollback to the former browser protocol requires reauthentication. Existing
opaque records deliberately cannot be converted back into usable refresh JWTs.
Keep the nullable column until rollback is complete; normal database backups and
the prior release artifacts remain the recovery mechanism. No production
migration or deployment is performed by implementing this change.
