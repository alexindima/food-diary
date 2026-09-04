# Identity infrastructure

Own Identity persistence adapters, including `UserLoginEventRepository` and `EmailTemplateProvider`. Preserve login-event search/date/deletion semantics and the provider's singleton lifetime, one-minute cache and locale fallback. Keep `UserRepository` with Users Infrastructure, and shared SSO/Redis storage, mail transport and cleanup with their established owners.

Google and Telegram validators/options now belong to Providers, preserving legacy
CLR names, signing/issuer/audience/lifetime rules and cancellation. API and JobManager
explicitly compose AddIdentityProvider; Initializer does not acquire these options.
Keep singleton lifetimes and supplied TimeProvider, and do not merge provider
registration into persistence or JWT/password/SSO registration. See
docs/ai/auth-storage-provider-ownership.md.

Register adapters through `AddIdentityPersistence`. Do not make central Infrastructure reference this adapter assembly; composition roots reference it explicitly.

`TelegramAssertionReplayGuard` is scoped and uses the shared context and configured
TimeProvider. Keep SHA256/UTF-8 fingerprinting, the two SQL statements, expiry
cleanup and ON CONFLICT behavior unchanged during physical relocation. The guard
does not replace signature/age validation in Telegram application/provider flows.

Own `JwtTokenGenerator` under Authentication and `PasswordHasher` under Services
(matching retained CLR namespaces). Register their
existing ports as singletons through `AddIdentityAuthenticationInfrastructure`,
separately from persistence. Preserve claims, signatures, expiry, refresh checks
and legacy/enhanced bcrypt compatibility. Keep JwtOptions/binding central and
Users credential operations with Users; relocation must not redesign security.

Own ordinary `AdminSsoService` under Authentication, registered as the existing
singleton by `AddIdentityAuthenticationInfrastructure`. Despite its name, the
service is consumed by Identity's AdminSsoStart/Exchange application flows; Admin's
impersonation adapter is a distinct protocol. Keep shared `IAdminSsoCodeStore`,
the central in-memory implementation and API Redis override outside this module.
Preserve 32-byte randomness, 43-character URL-safe codes, two-minute expiry,
validation before consume, GUID payload parsing and cancellation.
