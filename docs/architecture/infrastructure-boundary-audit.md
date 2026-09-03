# Residual central Infrastructure ownership

## Scope and method

Current-source audit of `FoodDiary.Infrastructure`, emphasizing `Persistence`,
after physical domain/application extraction. Baseline: `7ee80615`.
Checked implementations, interfaces, EF composition, module projects and host DI.
This is an ownership review, not an exhaustive security or performance audit.
Existing namespaces and folder names are not proof of ownership. The shared
database and cross-module read projections do not make every adapter shared.

## First tranche: Identity adapters

`UserLoginEventRepository` and `EmailTemplateProvider` move to existing
`Modules/Identity/Infrastructure/Persistence`, with their focused tests under the
module. `AddIdentityPersistence` owns the scoped repository/read/write aliases and
singleton template provider. API, Initializer and JobManager already call it.
The interfaces remain central and consumers continue to use them unchanged.

Login events and email templates already belong to Identity Domain. Joining Users
for reporting does not transfer the User aggregate or require a central repository.
SQL, tracking, paging, date filters, deletion batches, cancellation and the
one-minute template cache/locale fallback remain unchanged. Coordinated host
rebuilds are required: CLR namespaces are preserved, assembly ownership changes.

## Completed follow-up: Admin role-audit projection

The next bounded tranche moves AdminUserRoleAuditRepository to
`Modules/Admin/Infrastructure/Persistence/Admin`, with its scoped aliases and
focused tests. Users retains all role-audit entities/mappings; no query, schema or
HTTP change is introduced. See `docs/ai/admin-role-audit-persistence.md`.

## Remaining candidates (not moved in these tranches)

Telegram replay persistence is extracted to Identity Infrastructure and its
existing PersistenceModel project. The technical consumed-assertion record is
not a Domain aggregate. The shared model entrypoint, SQL bodies, fingerprinting,
expiry semantics and authentication callers remain unchanged. Focused provider
tests leave the mixed Dietologist class; see
`docs/ai/identity-telegram-replay-persistence.md`.

| Current source | Likely owner / next action | Required boundary proof |
| --- | --- | --- |
| `Persistence/Email/*` outbox stream | Retain shared technical delivery for now | Identity and Dietologist supply fully rendered messages; the queue owns no authentication/template policy. A communications module needs a separate design, not forced assignment to Identity. |
| Shared SSO store and JWT options | Retain technical/host seams | Ordinary SSO protocol now belongs to Identity; Admin's impersonation remains distinct. Both consume atomic one-time storage. API Redis selection and shared JWT configuration stay unchanged. |

## Intentionally shared or mixed

- `FoodDiaryDbContext` and its partial DbSets, design-time factory, migration
  history and snapshot remain the single EF composition/migration boundary.
  Module mappings continue to be installed explicitly.
- `EfUnitOfWork`, domain-event dispatch, database telemetry and generic PostgreSQL
  lock machinery are shared mechanisms, not feature policy.
- `OutboxProcessingEngine`, claiming and retry policy remain common mechanisms.
  `OutboxDeadLetterReplayService` is mixed: it explicitly knows four streams.
  A future stream-adapter seam needs a separate design; this is not solved by
  moving its file or weakening transactional replay guarantees.
- `RecipeCompositionTransactionLock` coordinates Products and Recipes; retain one
  shared lock identity until a deliberate composition boundary replaces it.
- `UserRepository` now belongs to Users Infrastructure together with its four
  scoped aliases. Its tracked aggregate access stays separate from administrative
  projections and access-token security-state reading. Identity consumes Users
  capabilities; no central repository compatibility seam remains.
- Audit storage/writer may remain a generic capability even when Dietologist's
  rule selection moves. Do not conflate storage with the rules that emit entries.
- `StronglyTypedIdConverters` retains public compatibility helpers. Audit current
  consumers separately before removing or relocating them; names alone are not
  evidence that a helper is unused.

## Recommended sequence

1. Finish and verify the two Identity adapters above.
2. Admin role-audit projection and Identity replay persistence are extracted;
   preserve their module tests and shared model/host composition boundaries.
3. Images/Gamification stream records and mappings now follow the existing
   Notifications precedent in their module PersistenceModel projects. The engine,
   claiming, replay and DbContext remain central; see
   `docs/ai/module-outbox-persistence.md` for the unchanged lifecycle boundary.
4. Dietologist audit rules and focused tests now live in its existing
   Infrastructure module. Central DI consumes EF's ISaveChangesInterceptor port
   after domain-event dispatch, and the module registers it once per scope.
   Shared AuditEntry/table/writer remain central. See
   `docs/ai/dietologist-audit-persistence.md`.
5. JWT generation and password algorithms are extracted to Identity with explicit
   singleton authentication registration in all three hosts. Users still owns
   credential operations; JwtOptions/API validation stay with their owners. Email
   outbox remains shared technical persistence/dispatch. See
   `docs/ai/identity-authentication-adapters.md`.
6. Ordinary SSO protocol and focused tests now belong to Identity, using its
   existing singleton authentication registration. Shared store/Redis and Admin
   impersonation stay outside the move; see `docs/ai/identity-sso-ownership.md`.
7. Review mixed replay and UserRepository seams independently; do not redesign
   them while performing physical relocation. Shared SSO storage is not an
   uncompleted Identity protocol move.
8. The independent access-token security reader is separated from UserRepository
   into Users Infrastructure, preserving its persisted no-tracking predicate.
   Other repository source/aliases remained shared in that tranche. See
   `docs/ai/users-security-reader-ownership.md`.
9. Both administrative read ports now share Users' UserAdministrationReadRepository,
   preserving original query bodies, no-tracking role loading, paging/search and
   model mapping. Central tracked lookup/write/Google remains unchanged. Review
   that remaining aggregate repository as one boundary before another move; do
   not split shared tracking by provider names alone. See
   `docs/ai/users-administration-reader-ownership.md`.
10. The complete remaining UserRepository and seven focused provider tests now
    belong to Users. Query/write bodies and scoped alias identity are preserved;
    new SQL regressions protect caller-controlled saving/transactions, role-audit
    rollback, account predicates and tracked state. See `docs/ai/users-repository-ownership.md`.

Next review the explicit four-stream dependencies in OutboxDeadLetterReplayService.
That is a shared-engine versus module-stream adapter design, not an automatic
physical move or permission to change replay/transaction semantics.

Every persistence tranche must keep the dependency graph acyclic, preserve model
identity, retain real provider tests and check composition roots. Avoid adding new
shared assemblies merely to hide a cycle.
