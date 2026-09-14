# Infrastructure Layer Guidelines

## Scope
Rules for `FoodDiary.Infrastructure/`.

## Responsibilities
- EF Core persistence, external integrations, and technical implementations.
- Implement abstractions declared in upper layers.

## Data Access
- Hydration is the runtime-context pilot (ADR 0040). Central migration mappings remain unchanged. ModuleContextSaveCoordinator owns multi-context saves, common connection/transaction, savepoints and execution-strategy retries; repositories never commit independently. Direct central SaveChanges rejects pending module changes. Include registered module trackers in transaction-entry guards and reset; preserve command telemetry and post-commit ordering.
- Keep the shared `DbContext`, migrations and model snapshot here. Module-owned entity configurations live in their PersistenceModel projects and are applied explicitly by the shared context. Shared audit, email-outbox, and replay-audit records/configurations live in their narrow Shared PersistenceModel projects; the central context applies them explicitly while the generic processing engines remain here.
- Use Fluent API for mapping and constraints.
- Keep migrations in this project.

## Rules
- Do not move domain rules from aggregates into persistence code.
- Keep dependency direction inward (Infrastructure depends on Application/Domain, not vice versa).
- Keep project references aligned with the enforced dependency matrix: approved shared/module contracts, Domain owners, PersistenceModel assemblies and shared primitives. Do not reference module adapter implementations, presentation projects, host projects, or resources.
- Keep shared external provider adapters that are not persistence concerns in `FoodDiary.Integrations`; Notifications-owned web-push adapters live in `Modules/Notifications/Infrastructure`.
- Resolve module-owned EF `ISaveChangesInterceptor` registrations after the
  explicit telemetry and domain-event interceptors. Dietologist owns collaboration
  audit rules/registration; central Infrastructure owns only generic audit storage.
  Do not reference the Dietologist interceptor type or adapter project here.
- Export PDF rendering and its safe image HTTP client belong to `Modules/Export/Infrastructure`; executable hosts register AddExportInfrastructure explicitly. Do not reintroduce QuestPDF or a central PDF implementation.
- Notifications configurations are registered explicitly from its PersistenceModel assembly; generic outbox processing/claiming/replay remain central and use the shared Outbox.Abstractions contract.
- Images and Gamification outbox records/configurations likewise belong to their existing PersistenceModel assemblies. Keep their DbSets and generic engine/claiming/replay here, but do not reintroduce duplicate central mappings or record classes.
- Generic audit interception, email dispatch/claiming, and multi-stream outbox replay coordination remain central. Their dependency-light EF records and configurations are owned by `FoodDiary.Audit.PersistenceModel`, `FoodDiary.Email.PersistenceModel`, and `FoodDiary.Outbox.PersistenceModel`; do not move the runtime engines into those model-only assemblies.
- Dead-letter replay uses scoped `IOutboxReplayStream` extensions in this existing Infrastructure assembly. Images, Notifications and Gamification own their list/find SQL and metadata; only the purged, non-replayable email adapter remains central. The coordinator owns validation, audit/reset/transaction and saves through IUnitOfWork so the audit and owner record commit atomically; it knows no concrete stream types. Module adapters use their owner contexts registered through CreateModuleContext on the shared connection and must not save or commit. Preserve explicit stream ordering and replay eligibility; see `docs/ai/outbox-replay-stream-boundary.md`.
- Keep repository-owned `SaveChangesAsync` and manual transactions inside the architecture-test allowlist. `AiQuotaRepository` is an explicit exception: its short PostgreSQL transactions atomically reserve or reconcile quota independently of the request transaction, and must never contain an external provider call.
- `EfModuleTransactionCoordinator` owns the existing top-level default-isolation transaction, clean-entry check, retry/reset and shared unit-of-work save/commit. WeeklyGoals supplies its user/week advisory-lock operation through IModuleTransactionCoordinator; keep notification delivery and external calls outside the transaction.
- Products delegates Serializable transaction ownership to IModuleTransactionCoordinator while retaining its Product row lock, usage check and mutation in one short transaction.
- Recipes delegates Serializable transaction ownership to IModuleTransactionCoordinator while retaining graph/usage checks and mutation in one short transaction.
- Do not reintroduce direct SMTP delivery configuration into the primary API/infrastructure path; MailRelay owns mail delivery runtime configuration.
- Keep retries/logging policies consistent with API composition.

## Commands
- Build: `dotnet build FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`
- Tests: `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj`

## EF Core Migrations
- Always commit both files for each migration: `*.cs` and `*.Designer.cs`.
- Add `[ExcludeFromCodeCoverage]` to the migration implementation class and model snapshot so generated EF code does not affect dotCover/code coverage.
- **CRITICAL**: `dotnet ef migrations add` generates Allman-style braces, but the project requires K&R style. After generating, you MUST:
  1. Remove `using System;` (implicit usings are enabled)
  2. Convert ALL `)\n{` and `=>\n{` patterns to `) {` / `=> {` â€” especially `constraints: table =>\n{`
  3. Strip UTF-8 BOM if present
  4. Ensure LF line endings (not CRLF)
- Before commit, run a whitespace formatter/check on migration files. Preferred command: `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`. This specifically avoids CI failures like `WHITESPACE: Fix whitespace formatting` in generated migrations.
- See `docs/backend/BACKEND_MIGRATION_SAFETY.md` for migration safety guidance.

## MealPlanning

MealPlanning repositories and DI live in Modules/MealPlanning/Infrastructure;
all six EF mappings live in its Model project and are applied explicitly by the
shared context. Hosts and repository-resolution fixtures call AddMealPlanningModule
in addition to AddInfrastructure. Central Infrastructure must not reference the
module adapter project. User cleanup belongs to `Modules/Users/Infrastructure`; DbContext and migrations remain here.

- Exercises mapping is registered through ApplyExercisesPersistenceModel; its repository and complete DI belong to Modules/Exercises/Infrastructure. Keep the central context, migrations and snapshot here.

- Dashboard projection readers live in Modules/Dashboard/Infrastructure; hosts explicitly call AddDashboardReadServices after AddInfrastructure. Shared context and migration ownership remain here.

## Recipes physical ownership

Recipes aggregate ownership lives under `Modules/Recipes/Domain`; IDs live in dependency-free `Domain.Contracts`. Recipe mappings preserve User, Product and MealItem relationships with unidirectional Fluent API mappings. Shared context/migrations/snapshot and mixed integration tests remain central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers.

## Admin physical ownership

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. Legacy application assembly and CLR namespaces
remain stable; compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned despite
legacy Admin namespaces. Shared context/migrations and SSO storage remain central;
JWT and ordinary SSO protocol implementations belong to Identity. HTTP authorization,
structured audit and MailInbox client bridge retain their established owners.
Hosts call AddAdminModule; JobManager adds only AddAdminPersistence. See
docs/ai/admin-ownership-inventory.md for current source evidence and test ownership.

AdminUserRoleAuditRepository is now registered and compiled by Admin
Infrastructure. Users retains role-audit entities/mappings; central Infrastructure
retains DbContext and migrations. Do not register the Admin projection centrally.

## Products physical ownership

Products aggregate, ProductId seam, use cases, ports, persistence adapters and EF
model live under `Modules/Products`. User/Product and Product/MealItem relationships
are mapped unidirectionally; shared context/migrations and transaction-attempt reset remain central. Hosts explicitly
compose AddProductsModule; JobManager adds AddProductsPersistence without new
handlers. See `docs/ai/products-ownership-inventory.md` for the boundary and
coordinated-rebuild compatibility promise.

## Meals physical ownership

MealRepository and the four Meals EF mappings live under `Modules/Meals`; the shared
context explicitly applies `ApplyMealsPersistenceModel`. UserConfiguration belongs
to Users Infrastructure/Model; user cleanup belongs to Users Infrastructure.
Keep Meals DbSets, migrations and snapshot central. Hosts use AddMealsModule;
JobManager uses AddMealsPersistence only. See `docs/ai/meals-ownership-inventory.md`.

## RecentItems physical ownership

RecentItems repository, post-commit recorder, DI and EF mapping live under `Modules/RecentItems`. The shared context applies its model; migrations/snapshot and post-commit queue/UoW stay central. See `docs/ai/recent-items-ownership-inventory.md`.

## Identity persistence adapters

JWT issuance/refresh validation and password hashing are registered by Identity's
`AddIdentityAuthenticationInfrastructure`, not central authentication DI. Central
JwtOptions/binding and the shared SSO store remain here. Identity also registers
ordinary AdminSsoService; Admin retains impersonation and the API selects Redis.
Do not reintroduce the ordinary SSO protocol registration centrally. The generic Email outbox remains
central: independent producers supply fully rendered envelopes; Identity does not
own every email delivery policy. See `docs/ai/identity-authentication-adapters.md`.
See `docs/ai/identity-sso-ownership.md` for protocol/store ownership and isolation.

Identity owns login-event persistence/reporting and cached email-template lookup
under `Modules/Identity/Infrastructure`. Do not register these adapters in central
DI; hosts already compose `AddIdentityPersistence`. UserRepository now belongs to
Users Infrastructure; generic persistence engine and central model/migrations remain here. See
`docs/architecture/infrastructure-boundary-audit.md` for the residual ownership plan.

Telegram replay guard/registration and consumed-assertion EF state now belong to
Identity Infrastructure/PersistenceModel. The context already installs the model
through `ApplyIdentityPersistenceModel`; do not add a second central mapping or
guard registration. JWT, SSO, provider validation and the combined UserRepository
are not part of that move.

## Users Domain ownership

`IUserAccessTokenSecurityReader` is now independently registered by Users
Infrastructure, not an alias of UserRepository. All four repository/lookup/Google/write
aliases now belong to Users Infrastructure on one scoped repository. The reader uses
the shared context but must query persisted state with no tracking. See
`docs/ai/users-security-reader-ownership.md`. Administrative read/model aliases share
Users' UserAdministrationReadRepository, preserving the original queries and DTO
mapping; do not register them centrally. See `docs/ai/users-administration-reader-ownership.md`.

The remaining UserRepository has moved as a whole to Users with exact query/write
bodies; central AddUserPersistence and its registration helper are removed. Keep
caller-owned SaveChanges and the shared context; see `docs/ai/users-repository-ownership.md`.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Users
Infrastructure owns UserRepository; authentication flows/providers, shared DbContext,
migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

SharedTransactionBoundary resets attempt-local tracking, domain-event dispatch state and post-commit actions for replayable module transactions. Callbacks must reload mutable inputs on each attempt. Six restrictive image FKs protect references against concurrent cleanup; Users releases its profile FK explicitly before image purge. See ADR 0032.

Outbox finalization is fenced by the original LockedBy concurrency token on all four streams. A lost claim must neither overwrite state nor release a newer claim. Gamification SQL completion/requeue also conditions on LockedBy and preserves revision coalescing. The InMemory fallback persists claims before dispatch. Replay wraps its short transaction in the provider execution strategy and resets tracking between failed attempts; external delivery is never retried by that transaction. See ADR 0033.

Gamification additionally fences EF completion/failure by Revision. On conflicts the module conditionally releases only a changed revision with the original LockedBy; shared processing saves the fallback and reports durable outcomes. See ADR 0034.

AI persistence boundary pilot: AI-owned entity configurations remain in its PersistenceModel; only its four foreign User/ImageAsset relationships are composed centrally by AiCrossModuleRelationships after module registrations. ScalarPersistenceBoundaryTests rejects foreign Domain assembly references in the compiled AI model. The exact project matrix and ModuleAggregateIsolationTests protect direct references and unchanged relational schema. See docs/ai/ai-persistence-boundary.md.

Hydration, RecentItems, Exercises and WeeklyGoals use the same scalar persistence boundary as AI: owned mappings stay local, typed User Cascade FKs live under Persistence/Composition and are installed after module registrations. ScalarPersistenceBoundaryTests and the exact matrix prohibit foreign Domain dependencies in these nine models. Preserve schema and lifecycle behavior; see docs/ai/scalar-persistence-boundaries.md.

Images, Cycles, BodyMetrics and Wearables also use scalar persistence boundaries. Their six User Cascade FKs live in typed central composers; preserve owned mappings, user isolation and schema. Central Infrastructure references Wearables.Domain directly for its two entity mappings.

ContentReports, Lessons, Gamification and Notifications also use central typed relationship composers for five User Cascade FKs. Their thirteen-model guardrail includes these four PersistenceModel assemblies; keep all owned mappings and same-owner relationships local.

Fasting and Billing extend the scalar boundary to fifteen models. Their six User Cascade FKs are configured in central typed composers. Preserve Billing payment-to-subscription SetNull and all same-owner Fasting mappings locally.

Admin and Identity extend the scalar boundary to seventeen models. Central AdminCrossModuleRelationships preserves both impersonation Restrict FKs; IdentityCrossModuleRelationships preserves login-event and refresh-session Cascade FKs.

RecipeCommunity extends the scalar model guard to eighteen assemblies. RecipeCommunityCrossModuleRelationships owns its three existing User/Recipe Cascade FKs. Keep the absent RecipeLike-to-Recipe FK unchanged; central Infrastructure references RecipeCommunity.Domain explicitly.

Dietologist extends scalar model protection to nineteen assemblies. Its ten User FKs are configured by DietologistCrossModuleRelationships, preserving seven Cascade, two Restrict and one optional SetNull relationship. Existing Dietologist.Domain reference suffices.

Users extends scalar model protection to twenty assemblies. UsersCrossModuleRelationships owns its optional profile-image ClientNoAction FK. Existing Users.Domain and Images.Domain references suffice; preserve ADR 0032 and caller-owned profile unlinking before image purge.

Favorites and MealPlanning extend scalar model protection to twenty-two assemblies. Central typed composers preserve six Favorites Cascade FKs and four MealPlanning relationships: optional MealPlan User Cascade, MealPlanMeal Recipe Restrict, ShoppingList User Cascade, and optional ShoppingListItem Product SetNull. Same-owner mappings, indexes, converters and source provenance stay local. Central Infrastructure references Products.Domain explicitly; no schema or API change is intended.

Products extends the scalar persistence boundary to twenty-three models. Its three foreign FKs live in central ProductsCrossModuleRelationships: optional ImageAsset ClientNoAction, optional UsdaFood SetNull and the unchanged conventional User relationship. The owner model uses ID-only Images.Contracts and Users.Domain.Contracts; UsdaFdcId needs no foreign contract. Central Infrastructure references Usda.Domain directly. Preserve indexes, converters, xmin and ADR 0032 image integrity; no schema or API change is intended.

Recipes and Meals extend scalar persistence protection to twenty-five models. Their ten foreign FKs live in RecipesCrossModuleRelationships and MealsCrossModuleRelationships; all optionality and delete policies remain unchanged, including four image ClientNoAction mappings. Owned nested Recipe Restrict and Meal/Ai cascades stay local. Recognition receipts retain their User Cascade FK and deliberately have no Meal FK. The models consume direct ID contracts; existing central Domain references suffice. No module PersistenceModel retains a foreign Domain project reference.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.

BodyMetrics is the second module with an owned runtime context (ADR 0040). Weight and waist repositories use only its owned sets; Users retains goals. Shared UoW coordinates central, Hydration and BodyMetrics contexts; central migration/read/purge mappings remain.

Exercises extends the owned runtime contexts to three modules (ADR 0040), sharing the same scoped connection and UoW. Central migration/read/purge mappings remain.

Cycles is the fourth runtime-context owner (ADR 0040). Its profile and seven child mappings share the existing UoW; central migration/read/purge bridges remain unchanged.

OutboxProcessingEngine and OutboxMessageClaimer accept a DbContext supplied by the owning adapter. They require a clean tracker and no existing transaction. For FoodDiaryDbContext, the clean-entry guard also checks every registered module context. Lease acquisition, retry policy, fencing, finalization and table allowlist remain shared. This technical seam does not change current adapter registrations or authorize a module to process foreign tables.

Coordinated module saves invoke central SaveChanges even when its tracker starts clean, so audit interceptors can stage central audit records. Preserve atomic rollback; do not copy all SaveChanges interceptors into owner contexts. The stateless Users Telegram conflict interceptor is explicitly installed on its owner context because that context persists the protected uniqueness constraints.

UsersDbContext saves at priority -100, before central priority 0 and other module contexts at default 100. Preserve stable ordering among peers and resolution-order domain-event discovery. Bind and release transactions for every non-central participant by identity; never assume central is array element zero. Users explicitly installs its Telegram conflict interceptor on the owner context. Central migrations, FK composition and technical purge bridges remain.

IModuleContextFactory is scoped to the same FoodDiaryDbContext instance by AddInfrastructure. Its implementation remains the existing CreateModuleContext method; preserve provider options, common connection, command interceptors and participant save order.

Billing uses the coordinator command overload to preserve unconditional unit-of-work save and owner exception translation before shared tracker reset. Never move Billing constraint names or entities into the shared coordinator.

EfModuleSessionCoordinator owns the separate session lease and single-attempt clean/reset/save boundary used by Wearables. It runs provider callbacks without a database transaction or execution-strategy replay; intermediate saves remain durable and only final persistence retries. Keep it distinct from EfModuleTransactionCoordinator.
