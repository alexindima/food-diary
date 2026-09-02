# Identity infrastructure

Own Identity persistence adapters, including `UserLoginEventRepository` and `EmailTemplateProvider`. Preserve login-event search/date/deletion semantics and the provider's singleton lifetime, one-minute cache and locale fallback. Keep the combined `UserRepository`, external provider adapters, JWT/SSO/Redis, mail transport and shared cleanup with their established owners.

Register adapters through `AddIdentityPersistence`. Do not make central Infrastructure reference this adapter assembly; composition roots reference it explicitly.
