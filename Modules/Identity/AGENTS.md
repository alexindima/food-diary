# Identity logical module

Telegram HTTP transport is split between legacy authentication, onboarding,
browser backup-email proof and bot operations controllers, with unchanged route
prefixes. Build application requests in HTTP mappings. Anonymous onboarding
methods are explicitly reviewed in ControllerConventionsTests; bot operations
remain protected by RequireTelegramBotSecret despite AllowAnonymous.

Identity owns authentication, account recovery, external login, token issuance,
login auditing, initial-admin bootstrap, and application email-template use cases.
Authentication and Email remain logical areas inside one application assembly.

Consumer email/template and login-event APIs live in Identity.Contracts.
Application.Abstractions retains internal repository and provider ports. Admin
uses IImpersonationTokenIssuer, not the generic JWT generator. JWT implementation
and claims remain owned by Identity Infrastructure.

Application, Domain, Contracts and Presentation now use their canonical project-and-folder namespaces.
Internal authentication/provider and repository ports belong to Identity Application.Abstractions; consumer email/template and login-event capabilities belong to Identity.Contracts. Shared email transport/outbox and IAdminSsoCodeStore remain central. The User/Role CLR graph and credential state belong to Users Domain. Users Infrastructure owns the tracked UserRepository; Identity application uses Users capabilities, not aggregate repository ports. Keep shared DbContext and migrations/snapshot central. External provider implementations and shared SSO/Redis storage,
MailInbox/MailRelay integration and hosts remain with their current owners. Identity-specific SignalR, refresh-cookie and Telegram-secret presentation adapters belong to `Modules/Identity/Presentation`; Google/Telegram validators and options are owned by Identity
Infrastructure/Providers with explicit AddIdentityProvider composition.
The ordinary `AdminSsoService` protocol now belongs to Identity Infrastructure;
shared in-memory/Redis single-consumption storage and Admin's impersonation protocol
retain their existing owners. See `docs/ai/identity-sso-ownership.md`.

Identity physically owns EmailTemplate, UserRefreshTokenSession, and UserLoginEvent
through its Domain project, plus their EF model and independent template/session
adapters. Central FoodDiaryDbContext applies the Identity persistence model; central
migrations/snapshot remain central. Identity Infrastructure also owns the login-event
write/cleanup repository and cached email-template provider. Login-event reporting
reads Users through the host-composed IUserLoginEventQuery adapter; Identity keeps
its scoped repository aliases. Register owner adapters with `AddIdentityPersistence`
and composed reads with `AddReadModelComposition`. See
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

IdentityDbContext owns runtime persistence for Identity records and template revisions. Central FoodDiaryDbContext remains the migration/composed-read model and shared transaction coordinator; tracked owner changes save through IUnitOfWork.

Infrastructure, PersistenceModel and Application.Abstractions now use project-and-folder namespaces. The latter two projects are siblings of Application and Infrastructure. All module layers use canonical project-and-folder namespaces. JwtOptions is a shared technical configuration type; central registration still binds and validates it. No module Infrastructure may reference central Infrastructure.

ADR 0044: hosts explicitly compose AddSharedAuthentication and AddIdentityEmailOptions. Identity owns email link option binding; shared authentication owns JWT binding and fallback SSO storage.

Use canonical FoodDiary.Modules.Identity project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.

Initializer dispatches BootstrapInitialAdminCommand from Contracts. Bootstrap retains its explicit save. Login-event cleanup dispatches CleanupLoginEventsCommand and rejects nonpositive batch sizes before independently committed deletes. Neither request uses the automatic transactional-command marker.
