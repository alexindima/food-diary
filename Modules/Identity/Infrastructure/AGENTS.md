# Identity infrastructure

Register IImpersonationTokenIssuer as a singleton alongside IJwtTokenGenerator.
Its adapter delegates to the existing impersonation access-token overload; do not
change claims, expiry, role handling or refresh validation during this extraction.

Own Identity persistence adapters, including `UserLoginEventRepository` and `EmailTemplateProvider`. Preserve login-event search/date/deletion semantics and the provider's singleton lifetime, one-minute cache and locale fallback. Keep `UserRepository` with Users Infrastructure, and shared SSO/Redis storage, mail transport and cleanup with their established owners.

Google and Telegram validators/options now belong to Providers, using module-owned CLR namespaces while preserving signing/issuer/audience/lifetime rules and cancellation. API and JobManager
explicitly compose AddIdentityProvider; Initializer does not acquire these options.
Keep singleton lifetimes and supplied TimeProvider, and do not merge provider
registration into persistence or JWT/password/SSO registration. See
docs/ai/auth-storage-provider-ownership.md.

Register adapters through `AddIdentityPersistence`. Do not make central Infrastructure reference this adapter assembly; composition roots reference it explicitly.

Identity registers its `IUserDataPurgeParticipant` through `AddIdentityPersistence`.
It deletes the removed user's Telegram operation journal inside the Users cleanup
coordinator's transaction, including terminal deduplication metadata. It does not
save or commit, reassign operations, or access another module's tables. The direct
Users.Contracts reference supplies this existing lifecycle extension point.

`TelegramAssertionReplayGuard` is scoped and uses the owner context, shared transaction and configured
TimeProvider. Keep SHA256/UTF-8 fingerprinting, the two SQL statements, expiry
cleanup and ON CONFLICT behavior unchanged during physical relocation. The guard
does not replace signature/age validation in Telegram application/provider flows.

Own `JwtTokenGenerator` under Authentication and `PasswordHasher` under Services
(using project-and-folder CLR namespaces). Register their
existing ports as singletons through `AddIdentityAuthenticationInfrastructure`,
separately from persistence. Preserve claims, signatures, expiry, refresh checks
and legacy/enhanced bcrypt compatibility. Keep JwtOptions in Shared/FoodDiary.Authentication.Contracts/Options with binding central and
Users credential operations with Users; relocation must not redesign security.

Own ordinary `AdminSsoService` under Authentication, registered as the existing
singleton by `AddIdentityAuthenticationInfrastructure`. Despite its name, the
service is consumed by Identity's AdminSsoStart/Exchange application flows; Admin's
impersonation adapter is a distinct protocol. Keep shared `IAdminSsoCodeStore`,
the central in-memory implementation and API Redis override outside this module.
Preserve 32-byte randomness, 43-character URL-safe codes, two-minute expiry,
validation before consume, GUID payload parsing and cancellation.

Login-event composed reads implement IUserLoginEventQuery in host read composition. UserLoginEventRepository retains AddAsync and bounded retention and delegates compatible read methods to the query port. Preserve existing scoped repository aliases; hosts register AddReadModelComposition. No authentication validation or token behavior changes.

IdentityDbContext owns six root types plus EmailTemplate owned revisions, using the unchanged Identity model. Registration shares the central connection and synchronizes the live transaction before repository and Telegram operations, including after intermediate UOW saves. Repositories accept narrow owner DbSets; Telegram stores use the typed owner context. Shared IUnitOfWork commits tracked changes atomically with Users. The singleton template provider opens an owner scope and retains its one-minute cache. The user-purge participant remains a reviewed shared-transaction bridge.

Registration obtains the live transaction from IModuleTransactionCoordinator,
without resolving FoodDiaryDbContext. Preserve the relational guard and pass the
operation cancellation token to UseTransactionAsync. Purge uses IdentityDbContext
and binds the live coordinator transaction on every invocation. Preserve order 130,
scalar user filtering and journal/deduplication deletion. Users retains transaction
completion; There is no central Infrastructure reference. Shared authentication options and HTTP dependencies are referenced explicitly.
