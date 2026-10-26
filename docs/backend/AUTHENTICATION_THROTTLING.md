# Authentication throttling

FoodDiary uses separate request budgets and a password-specific account guard.
This avoids exhausting every sign-in method after a few requests from a shared
office, mobile-network or VPN address.

## Policy

- Password, Google and Telegram each allow 60 requests per minute per source IP
  on each API instance. These guards run before expensive credential validation.
  Trusted forwarded-header processing determines the source address; raw request
  headers and client-supplied user IDs cannot select another partition.
- Password failures share a counter across IP addresses for the normalized email
  (`Trim().ToUpperInvariant()`). This normalization only selects the guard key;
  it does not change the existing account lookup rules.
- Ten invalid-credential results start a 30-second cooldown. Each additional
  verified failure doubles it: 30, 60, 120, 240, 300 seconds, capped at 300.
  The threshold failure still returns the ordinary credentials error. Subsequent
  requests during the cooldown return HTTP 429 before password verification.
- Successful password verification clears the failure history. Otherwise it
  expires after 900 seconds without a verified credential failure. Requests
  rejected during cooldown do not extend either deadline. Cancellation,
  dependency exceptions and non-credential account errors do not add failures.
- A temporary password cooldown leaves Google, Telegram, recovery and existing
  sessions available under their own limits. There is no permanent account lock.
  Knowledge of an email can still be used to cause temporary password-only
  denial of service; short capped cooldowns limit its impact. A future challenge
  mechanism would need a separate product and provider decision.
- Registration, password recovery, verification, linking and other legacy auth
  operations retain the `RateLimiting:Auth` settings, with a separate partition
  per controller action. Telegram onboarding uses the Telegram request budget;
  its configuration GET keeps its existing independent read budget.

The values are starting operational settings, not prescribed industry thresholds.
Review rate-limit rejections and legitimate login failures before adjusting them.
Changing these budgets does not replace provider signature, issuer, audience,
expiry, browser-binding or replay checks.

## State and concurrency

Identity's `IPasswordLoginThrottle` port is called by `LoginCommandHandler`.
Infrastructure owns the adapters. The API selects `RedisPasswordLoginThrottle`
when `ConnectionStrings:Redis` is configured. There is no fallback to an empty
local counter if Redis fails. Without Redis, the development fallback keeps at
most 10,000 email entries and rejects additional entries until capacity recovers.
Its expired entries are removed lazily during admission, with a periodic sweep
on incoming requests; it has no background cleanup worker.

The Redis key contains a SHA-256 fingerprint of the normalized email. Values
contain only counts, deadlines and an opaque attempt owner token. No password,
email, token or source IP is stored in this state or emitted in logs. Email
fingerprints are pseudonymous identifiers, not anonymized data. Redis permissions,
persistence, eviction and backup policy still apply to them.

Redis scripts use the Redis clock and atomic hash operations. One expiring lease
admits password verification for an email at a time; concurrent requests receive
429 with a one-second retry hint. The default lease is 30 seconds. Completion
checks both owner and expiry, so a stale request cannot reset a newer attempt's
failures or issue tokens after losing ownership. Expiring keys release abandoned
attempts and remove idle history independently of application transactions.

Successful login and ordinary expiry delete the state. Dependency/cancellation
cleanup releases an acquired lease without adding a failure. A cancellation
after a Redis command has already been submitted cannot undo that command;
expiry and ownership checks bound abandoned in-flight state.

## Configuration and HTTP compatibility

`PasswordLoginThrottle` binds validated options for `FailureThreshold`,
`FailureWindowSeconds` (idle failure-history retention), `InitialCooldownSeconds`,
`MaximumCooldownSeconds`, `AttemptLeaseSeconds` and `MaximumTrackedEmails`
(development adapter only). The cooldown cannot exceed failure-history retention.
`RateLimiting:PasswordLogin`, `RateLimiting:GoogleLogin` and
`RateLimiting:TelegramLogin` configure the independent HTTP guards. All queues
default to zero; rejected attempts do not hold open connections waiting for a slot.
An existing `RateLimiting:Auth` override no longer sets these three login budgets;
operators requiring custom login limits must set the corresponding new sections.

Both HTTP and account guards return the existing `RateLimit.Exceeded` error
envelope and `Retry-After` in seconds. Account retry metadata is mapped by the
login result filter into the header, without adding response fields. Routes,
successful authentication payloads and cookies remain compatible. No database
migration or new secret is required. The change takes effect on deployment;
deployment itself is outside this implementation task.

Monitor HTTP 429 responses by authentication route to include both kinds of
rejection. The existing middleware rate-limit counter covers the HTTP request
guard; account cooldown responses remain visible in the ordinary HTTP metrics.

## References and verification

- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html#login-throttling)
  recommends account-associated failure counters and discusses exponential
  lockouts and the risk of attackers deliberately locking another account.
- [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b/authenticators/)
  addresses account/authenticator failure limiting, increasing waits and resetting
  counters for authenticators used successfully. Its upper bound is not a suggested
  request-per-minute quota for this application.
- [Auth0 suspicious IP throttling](https://support.auth0.com/center/s/article/Understanding-and-Troubleshooting-IP-Throttling-Caused-by-Multiple-User-Login-Failures)
  documents the shared-network tradeoff of limiting failed attempts against many
  usernames from the same source address.
- [Amazon Cognito password lockouts](https://docs.aws.amazon.com/cognito/latest/developerguide/authentication-flow-things-to-know.html)
  provides a production example of increasing temporary password lockouts.

Owner application tests cover admission, outcomes, cancellation and lost leases.
Owner infrastructure tests cover normalization, cooldown growth/cap/reset, expiry,
concurrency and bounded memory. Host Redis integration tests exercise shared atomic
state and stale ownership against real Redis. HTTP tests cover independent budgets,
password cooldown headers and successful login against PostgreSQL. The existing
OpenAPI and error snapshots verify compatibility.
