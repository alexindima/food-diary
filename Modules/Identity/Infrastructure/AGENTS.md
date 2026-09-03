# Identity infrastructure

Own Identity persistence adapters, including `UserLoginEventRepository` and `EmailTemplateProvider`. Preserve login-event search/date/deletion semantics and the provider's singleton lifetime, one-minute cache and locale fallback. Keep the combined `UserRepository`, external provider adapters, SSO/Redis, mail transport and shared cleanup with their established owners.

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
