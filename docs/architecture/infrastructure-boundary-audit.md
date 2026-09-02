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

| Current source | Likely owner / next action | Required boundary proof |
| --- | --- | --- |
| `Persistence/Authentication/TelegramAssertionReplayGuard.cs`, consumed-assertion record and mapping | Identity | Preserve fingerprinting, expiry cleanup and atomic conflict handling; rerun security/real PostgreSQL tests. |
| `Persistence/Images/ImageObjectDeletionOutboxMessage.cs` and mapping | Images PersistenceModel | Follow Notifications' model-only project plus shared outbox contract; never create context-to-adapter cycles. |
| `Persistence/Achievements/AchievementEvaluationOutboxMessage.cs` and mapping | Gamification PersistenceModel | Preserve pending revisions, claim release and retry semantics; processor/enqueue already belong to Gamification. |
| `Persistence/Interceptors/CollaborationAuditInterceptor.cs` | Dietologist-specific audit rules | It switches explicitly on invitation, recommendation, client task and bulk dispatch. Move rule ownership without making central DI reference module adapters; preserve SaveChanges timing. |
| `Persistence/Email/*` outbox stream | Decide Identity/email capability boundary | Used by multiple modules; separate stream-specific records/dispatch from the generic processing engine. Shared consumption alone is not ownership. |
| `Authentication/*`, JWT options and password hashing | Separate Identity/Users boundary review | Authentication orchestration, credential hashing and shared caches have different consumers; do not move solely by directory name. |

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
- `UserRepository` implements user, Google identity, admin read-model and access
  token security ports. Split responsibilities only after proving shared tracking,
  deleted-user filters and scoped aliases. A blind move is not the next step.
- Audit storage/writer may remain a generic capability even when Dietologist's
  rule selection moves. Do not conflate storage with the rules that emit entries.
- `StronglyTypedIdConverters` retains public compatibility helpers. Audit current
  consumers separately before removing or relocating them; names alone are not
  evidence that a helper is unused.

## Recommended sequence

1. Finish and verify the two Identity adapters above.
2. The Admin read projection is extracted; review Identity replay persistence as
   the next separately verified change.
3. Move Images/Gamification stream records and mappings using the existing
   Notifications precedent, keeping the engine central.
4. Design Dietologist audit registration, mixed replay and UserRepository seams
   independently. Do not redesign them while performing physical relocation.

Every persistence tranche must keep the dependency graph acyclic, preserve model
identity, retain real provider tests and check composition roots. Avoid adding new
shared assemblies merely to hide a cycle.
