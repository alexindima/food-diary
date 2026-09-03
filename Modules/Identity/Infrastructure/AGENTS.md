# Identity infrastructure

Own Identity persistence adapters, including `UserLoginEventRepository` and `EmailTemplateProvider`. Preserve login-event search/date/deletion semantics and the provider's singleton lifetime, one-minute cache and locale fallback. Keep the combined `UserRepository`, external provider adapters, JWT/SSO/Redis, mail transport and shared cleanup with their established owners.

Register adapters through `AddIdentityPersistence`. Do not make central Infrastructure reference this adapter assembly; composition roots reference it explicitly.

`TelegramAssertionReplayGuard` is scoped and uses the shared context and configured
TimeProvider. Keep SHA256/UTF-8 fingerprinting, the two SQL statements, expiry
cleanup and ON CONFLICT behavior unchanged during physical relocation. The guard
does not replace signature/age validation in Telegram application/provider flows.
