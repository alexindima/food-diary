# Browser sessions

The user website and admin UI authenticate through opaque HttpOnly cookies in
the existing API (ADR 0058). Telegram's server bot retains bearer authentication.

Production Nginx uses host networking and reaches the bridge-network API through
the Compose gateway. Before recreating API, deployment runs
`python3 scripts/deploy/configure_forwarded_proxy.py` from `/opt/fooddiary`.
It discovers the existing default bridge through the running Compose PostgreSQL
container and atomically writes `.forwarded-proxy.env`, loaded only by API.
Only the exact gateway addresses are trusted, with a one-hop forwarding limit;
the whole Docker subnet is not trusted. Regenerate this file after recreating the
network, including during manual deployment. Missing or ambiguous topology stops
deployment before database writers are stopped.

Correcting a deployment that previously saw HTTP changes cookie names to the
HTTPS `__Host-` prefix. Previously issued unprefixed cookies require a fresh
sign-in after this rollout; do not accept weaker legacy cookie names on HTTPS
as a workaround.

Without this trust, API sees HTTP behind TLS termination and rejects the HTTPS
admin origin with `403 Authentication.InvalidBrowserRequest` before consuming
the SSO code. The ordinary site's CORS allowlist can hide this topology mistake.
Keep production admin same-origin validation enabled. A rejected SSO attempt
removes its code from the recovery return URL, so the access-denied page cannot
automatically exchange the same code again.
After Nginx readiness, deployment probes this endpoint through the local TLS
proxy with the real admin origin and an intentionally invalid code. It requires
the invalid-code `401`; an origin-validation `403` fails deployment.

Password failure cooldowns and the independent request budgets for each sign-in
method are documented in [Authentication throttling](AUTHENTICATION_THROTTLING.md).

## Sign-in and restoration

1. The existing password, Google, account restore, password reset or browser
   Telegram flow verifies the user's proof and issues an internal Identity
   authentication result.
2. `AuthenticationTokenService` selects browser, admin-browser or bearer issuance
   from the server-created client context. Browser issuance creates the opaque
   session directly, without constructing an access/refresh JWT pair. The HTTP
   filter only writes its cookie and public metadata. The credential is a session
   GUID plus a random 256-bit secret; only its SHA-256 hash is stored. Replacing a
   cookie revokes its previous proven session, preserving other devices. The
   former JWT conversion path remains an internal compatibility adapter.
3. The JSON response contains `user`, `roles`, `sessionId` and `rememberMe`.
   `accessToken` and `refreshToken` are absent for ordinary browsers.
4. On reload, `POST /api/v1/auth/session` proves the cookie and returns fresh
   metadata. The frontend does not trust stored role or identity hints as proof.
   A changed session or user invalidates a running tab rather than rebinding it.

Each authenticated request verifies the secret, active registry record, expiry,
current account and security version. Restoration may synchronize a changed
security version after reading the current principal. Critical operations that
revoke sessions cannot be bypassed by restoration.

Ordinary authentication reads a no-tracking session-proof projection and a
Users-owned scalar principal projection. It does not hydrate the User aggregate,
password or profile. The principal query returns current roles, account/security
state and trial dates in one SQL statement. Full sign-in and restoration reuse
the same Users domain privilege policy, including exact trial boundaries and
access-ticket caps. There is no principal cache. The real PostgreSQL session-list
regression measures four SELECTs: two authentication reads plus the endpoint's
current-session guard and list projection (previously five SELECTs).

Ordinary browser sessions start with a 30-day lifetime, or 90 days with remember-me.
Remember-me also makes the cookie persistent; otherwise it is a browser session
cookie (browser session restoration settings can preserve such cookies).
An independent persistent HttpOnly cookie, `fooddiary.browser.previous` (or
`fooddiary.admin.previous`), remembers the last issued session for that browser
profile for 365 days. HTTPS applies the same `__Host-` prefix and host-only scope.
Its session ID is protected with a time-limited, realm-specific Data Protection
purpose using the host's existing key ring. The marker cannot authenticate or
restore a session. After a fresh successful sign-in, Identity revokes the marked
session for the authenticated account within the login transaction, then issues
a new marker. This still works when the ordinary authentication cookie was lost.
Successful restoration backfills the marker for existing installations without
renewing a nonrenewed authentication cookie. Logout leaves the marker available;
its session has already been revoked.

The marker is scoped to a browser profile, with separate purposes for the website
and admin UI. User-Agent and IP address are display/audit metadata, not device
identity. Clearing all site cookies or using a new private profile removes the
association. Historic orphaned sessions without retained proof cannot safely be
assigned to one of several identical browsers; use the explicit session-revocation
controls to remove them. Revoked and expired records disappear from the active
list immediately and retain the existing database retention policy.

When at most seven days remain, successful `POST /auth/session` restoration can
atomically extend expiry by another 30/90 days. There is no absolute maximum
lifetime by default (`BrowserSessions.AbsoluteLifetimeDays` is null): continued
activity can preserve the session for years. An inactive session still expires
at its current deadline, and revoked sessions cannot renew. Configure the renewal
window in `BrowserSessions`; an absolute cap is available only as an explicit
operator policy override. With an unlimited policy, a stolen credential can also
remain renewable until it is revoked; HttpOnly/HTTPS, CSRF protection and session
revocation remain necessary.
Renewal is batched inside its window, rather than updating the deadline on every
request; the remaining idle time is therefore measured from the last renewal.
Remembered cookie expiry is renewed with the same credential; ordinary cookies
remain nonpersistent. The secret is not rotated. Concurrent restoration uses
compare-and-set plus a fresh proof check, never a stale tracked save over logout.
The eligibility flag is persisted separately: admin and unclassified sessions
cannot acquire renewal from a client header. The renewal migration enables known
existing ordinary browser providers; token sessions and admin SSO remain fixed.

Angular checks a known ordinary session on pointer/keyboard activity or becoming
visible, at most hourly. It does not keep an unused background PWA alive. Startup
restoration also renews when eligible. Restoration shares the sign-in/logout
cookie queue and Web Lock, pins the known public session selector, and discards
requests whose local identity changed while queued. Restoration has a 30-second
request timeout so an unavailable API cannot indefinitely hold
the cookie queue ahead of a new sign-in or logout. Automatic activity renewal
does not apply to Telegram bearer or impersonation contexts. No new wire fields
or endpoint are required. A deleted browser cookie still requires sign-in, and
expired or revoked sessions cannot be revived by restoration.
Ordinary authorized HTTP activity updates the displayed last-active time at most
once per five minutes. Its atomic update checks owner, proof, security version,
revocation, expiry and the prior activity cutoff. Concurrent requests cannot
repeatedly touch the row or save stale tracked state over logout. Activity does
not rotate the proof or extend expiry; renewal occurs only through restoration.
Computer, phone and separate
browser profiles get separate records. Tabs in one browser share the ordinary
cookie. Logout revokes the selected record; the session-management endpoints can
revoke another device or all other sessions.

`UserSessionCleanupJob` dispatches Identity's `CleanupSessionsCommand`. By default,
records remain for 30 days after revocation or expiration, then daily cleanup
deletes up to 20 independently committed batches of 500 records. Legacy rows
without explicit expiry use the same configured regular/remembered lifetime as
access checks. Active and recently terminated records remain; deleting a revoked
record cannot restore access. The job has cancellation, retries, concurrency
exclusion and the shared execution observer. Configure it with `UserSessionCleanup`.

## Temporary failures and logout recovery

Session restoration and logout have separate rate policies (default 120 and 30
requests per minute), independent of credential sign-in limits. A restore 401/403
rejects the session; network, 429 and server failures leave recovery retryable.
The protected route waits and the UI offers retry, respecting numeric Retry-After.

The admin app exposes the same temporary-unavailable state on its unauthorized
page, with retry back to the requested route. It retains known public metadata
while unavailable but does not treat that metadata as a successful permission
check. Restorations are coalesced, time out after 30 seconds and pin the current
public session ID. A late restoration or protected-request rejection cannot
replace a newer SSO selection. The dietologist guard also waits for restoration.

Logout clears local identity immediately and records pending intent containing
only public session/context selectors and transport. With available browser
storage, this intent survives reload and prevents implicit cookie restoration
until logout is confirmed or the server proves a replacement session. Website
and Mini App intents have separate local-storage keys; impersonation intent is
tab-scoped. Requests pin the original selectors, and late results cannot change
a fresh login. Failed server revocation retains the cookie so it can be retried;
the server expires it only after a successful logout result. A browser cannot
confirm remote revocation while the API is unreachable.

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
- Explicit Mini App logout validates the expected session against the signed
  refresh proof, independently of any ordinary website cookie. Missing proof is
  not acknowledged as a replaced session; a mismatch cannot clear the new cookie.

Normal browser credentials cannot mint JWTs through `/auth/refresh`. Conversion
replaces the stored refresh proof and refresh handling rejects browser records.
Legacy stored browser tokens are discarded on startup.

## Admin and impersonation

Admin SSO issues its own opaque cookie and leaves the main site's cookie intact.
Direct issuance uses the configured access-token lifetime and the current account
access cap; the ordinary 30/90-day browser-session lifetime does not extend
administrator access. The compatibility converter still caps its session by the
original internal SSO access ticket.
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
expiry remains bounded by the configured access-token connection lifetime,
registry expiry and account access cap. `SessionSecurityHubFilter` additionally
rechecks every open connection every 15 seconds, with a five-second verification
deadline and an independent persistence scope. Rejection or unavailable security
state aborts the connection even if the client invokes no hub methods. Checks
include the originating administrator/session for impersonation. Disconnect
releases the monitor; reconnect authenticates again. This works against shared
database state on each API replica and does not provide a SignalR backplane.

## Telegram

Bot-secret authentication still returns a bearer JWT for server operations.
Explicit Mini App authentication requests with `X-FoodDiary-Client: telegram`
retain the existing refresh-cookie protocol and return an access JWT held only
in frontend memory. This includes fresh password/provider proofs used inside the
Mini App. Protected requests in this mode never fall back to the website cookie
when their bearer is missing. Browser Telegram OIDC without that mode produces an
ordinary browser session. A client header alone never replaces Telegram proof.

Mini App browser binding and third-party cookie requirements are unchanged.
On a fresh Mini App launch, anonymous users are routed to the Telegram entry
with the launch fragment intact. A newly verified `login` intent completes
automatically for an already linked account. Registration, linking and restored
tickets retain their explicit actions. Ordinary website visits and OIDC callbacks
keep their existing flows.

A tab-scoped public marker preserves the Telegram transport after SPA navigation
and reload; the marker never authenticates a request. Explicit logout suppresses
automatic sign-in for that launch, including reloads, using a SHA-256 digest of
the launch proof in session storage. The application does not persist the proof
for this purpose. A newly opened launch with different proof can sign in again.
Pending logout and session-recovery failures take precedence over automatic
sign-in. Rejection of a fresh Mini App proof does not trigger session refresh.

Local integration tests cannot establish Telegram Web, iOS or Android cookie
compatibility; those device flows require the Telegram test stand.

## Release and rollback

Apply `20261025190554_AddBrowserSessions` before enabling the new API. It adds a
nullable `BrowserSecurityVersion` to the existing session table; it does not
rewrite existing sessions or create another store.

Apply `20261026035544_EnableBrowserSessionRenewal` before publishing the renewal
API. It adds a nonnullable eligibility flag with a false default and enables only
unrevoked, unexpired, recognized ordinary browser records. It preserves their
credential hashes and existing expiry; actual extension requires a later proven
restoration. Unknown records retain fixed expiry until a new sign-in.

Publish matching API and user/admin bundles plus the admin API proxy. Check login, reload, independent
devices, scoped logout, admin SSO, isolated impersonation and bot authentication.
Publish the main site's generated CSP metadata, SSR script-hash manifest and
finalized `ngsw.json` from the same build; see [script CSP](../frontend/SCRIPT_CSP.md).
API replicas must use the same session lifetime policy.

Do not log cookies, credentials, cached JWTs or authentication request bodies.
The new mediator request models redact credentials from JSON and ToString.
Public selectors and response metadata must not be treated as proof.

Rollback to the former browser protocol requires reauthentication. Existing
opaque records deliberately cannot be converted back into usable refresh JWTs.
Keep the nullable column until rollback is complete; normal database backups and
the prior release artifacts remain the recovery mechanism. No production
migration or deployment is performed by implementing this change.

## Verification boundaries

`SessionReplicaPersistenceApiTests` exercises independent API hosts against real
PostgreSQL. Ordinary credentials survive host disposal and a different Data
Protection application name, while revocation reaches another replica. A second
scenario uses real Redis and a shared file key ring to restore a protected
support context after restart, consume an SSO code once across replicas and
remove only the selected support context.

The client and admin `e2e/*-smoke/auth-recovery.spec.ts` scenarios cover transient
restoration, keyboard retry, Russian narrow-screen rendering, pending logout on
reload and replacement-account sign-in in another tab. Typed API fixtures prove
rendered client behavior; they do not substitute for the PostgreSQL HTTP tests.
An owned Chrome run also checks an actual older Service Worker upgrading to the
current build and verifies that API responses are absent from its caches.

These checks do not establish installed iOS/Android PWA lifecycle behavior,
Telegram embedded-cookie support, live Google/Paddle flows or the deployed
proxy/key-ring/Redis configuration. Verify those on the intended release stand.
