# Identity logical module

Identity owns authentication, account recovery, external login, token issuance,
login auditing, initial-admin bootstrap, and application email-template use cases.
Authentication and Email remain logical areas inside one application assembly.

Preserve the legacy `FoodDiary.Application.Identity` assembly and CLR namespaces.
Keep shared authentication/email contracts central. The User/Role CLR graph and credential state belong to Users Domain. Users Infrastructure owns the tracked UserRepository; Identity application uses Users capabilities, not aggregate repository ports. Keep shared DbContext and migrations/snapshot central. External provider implementations and shared SSO/Redis storage,
MailInbox/MailRelay integration, HTTP transport, and hosts remain with their current
owners. The ordinary `AdminSsoService` protocol now belongs to Identity Infrastructure;
shared in-memory/Redis single-consumption storage and Admin's impersonation protocol
retain their existing owners. See `docs/ai/identity-sso-ownership.md`.

Identity physically owns EmailTemplate, UserRefreshTokenSession, and UserLoginEvent
through its Domain project, plus their EF model and independent template/session
adapters. Central FoodDiaryDbContext applies the Identity persistence model; central
migrations/snapshot remain central. Identity Infrastructure also owns the login-event
reporting/cleanup repository and cached email-template provider. Their projections
may read Users data through the shared context; that read does not transfer User
ownership. Register all adapters with `AddIdentityPersistence`. See
`docs/ai/identity-domain-extraction.md`.

Telegram assertion replay persistence belongs to Identity Infrastructure. Its
technical consumed-assertion record and EF mapping belong to PersistenceModel,
not Domain. Preserve fingerprinting, expiry cleanup and atomic conflict behavior;
signature/age validation remains with the existing application/provider callers.

JWT generation/refresh validation and password-hash algorithms belong to Identity
Infrastructure. Hosts compose `AddIdentityAuthenticationInfrastructure` beside
`AddIdentityPersistence`. Users still owns credential operations and stored hashes;
it consumes the existing password-hasher port. JwtOptions/configuration and API
bearer validation retain their current owners.

The same authentication registration owns the singleton `IAdminSsoService`.
Preserve code encoding, two-minute TTL, validation-before-consumption and GUID
payload semantics. Do not register the shared store here or override host Redis selection.
