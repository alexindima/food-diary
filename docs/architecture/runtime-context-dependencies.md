# Remaining shared runtime context dependencies

Source inventory after module context namespace normalization. Scope: production C# under `Modules/` containing the `FoodDiaryDbContext` identifier; test sources are excluded. This is not a list of every persistence capability: inferred DbSets, generic DbContext access and calls through other services are additionally reviewed in `persistence-capabilities.json` and its architecture tests.

All 29 runtime contexts use `FoodDiary.Modules.<Module>.Infrastructure.Persistence`. The ten normalized contexts keep their assembly names, mappings, tables and transaction behavior. The nine context files whose projects retain legacy roots have scoped IDE0130 exceptions because that rule infers the old project root; Admin has also normalized its project root and needs no exception. RuntimeContextNamespaceTests instead enforces their exact module ownership across all contexts. Project-wide legacy RootNamespace values remain for other implementation types; this step intentionally does not rename provider types or migration snapshots.

## What remains

| Category | Files |
|---|---:|
| Audit bridge | 1 |
| Connection/lock | 0 |
| Transaction/provider registration | 0 |
| Transaction coordination | 0 |
| User purge | 4 |

The presence of 29 owner contexts does not mean all runtime access has left the shared context. Dashboard body reads now live in host ReadModel.Composition. Image reassignment now uses ImagesDbContext on the caller connection. The three module replay streams also use owner contexts; their coordinator saves through IUnitOfWork. Audit retains reviewed shared-context behavior.

## Next changes, in order

1. All 29 registrations now use IModuleContextFactory for owner creation. Shared/FoodDiary.Persistence.Abstractions has no project dependencies; FoodDiaryDbContext implements it with the unchanged scoped provider/connection and tracker registration.
2. Billing checkout now acquires IModuleSessionLock; its independent session, owner key and caller-controlled lease lifetime are preserved. Billing no longer references central Infrastructure.
3. Revisit purge and collaboration-audit bridges after transaction coordination is explicit. Preserve deletion order, FK behavior and audit atomicity.

The shared FoodDiaryDbContext partials and mapping composition remain necessary for the unified migration model and central read/purge integrations. Their removal is a separate architectural change, not part of a namespace rename.

## Complete direct-reference inventory

| Source | Category | Responsibility |
|---|---|---|
| [Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs](../../Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs) | Audit bridge | Inspects shared and Dietologist trackers, stages shared AuditEntry rows; preserve atomic save and event deduplication. |
| [Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs](../../Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs](../../Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs](../../Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs](../../Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs) | User purge | User cleanup orchestration, user/role deletion, locks and profile-image unlinking. |

WeeklyGoals no longer consumes FoodDiaryDbContext or central Infrastructure. Its live transaction accessor and advisory-lock runner use IModuleTransactionCoordinator; shared clean-entry/retry/reset/save/commit behavior remains in EfModuleTransactionCoordinator.

Meals recognition now also uses IModuleTransactionCoordinator. Its intermediate IUnitOfWork flush retains the owned Meal xmin and receipt atomicity. The Meals purge participant now uses its owner context and live coordinator transaction.

Products and Recipes delegate Serializable mutations and live transaction access to IModuleTransactionCoordinator. Their ordered purge participants still retain central context access.

Billing delegates transaction/save/retry/reset and live transaction access to IModuleTransactionCoordinator; duplicate translation remains owner-side before cleanup. Its separate checkout-session advisory lease uses IModuleSessionLock without exposing the central context.

Wearables no longer references FoodDiaryDbContext or central Infrastructure. IModuleSessionCoordinator owns its separate session advisory lease, single callback execution and clean/reset/final-save boundary. Intermediate saves remain durable and provider calls are never replayed.

RecentItems registration uses IModuleTransactionCoordinator for live transaction access. Its user-purge participant uses its owner context and live coordinator transaction; post-commit recording and shared unit-of-work saves remain unchanged.

Users and Identity registrations now synchronize through IModuleTransactionCoordinator. Identity purge now uses its owner context; Users retains cleanup orchestration. OpenFoodFacts also uses the live coordinator callback and has no direct or transitive central Infrastructure dependency. Ai prompt synchronization and Gamification stores/enqueue use the same contract; independent provider options and worker clean-entry coordination use the narrow contracts described below.

Outbox registration guards in Gamification, Images and Notifications now use IModuleScopeGuard. Its scoped central adapter checks the same live shared transaction and all registered trackers without saving or clearing them. The three modules retain the generic outbox engine dependency, but no longer resolve FoodDiaryDbContext in registration.

Ai registration now gets independent options through IIndependentModuleContextOptionsFactory. Central infrastructure copies the same configured provider/core extensions without reading the live scoped context or registering a participant. Quota and job operations retain independent contexts and commits; prompt writes still join the shared transaction. No registration source now references FoodDiaryDbContext. The direct module inventory contains 5 files: four purge sources and the collaboration audit interceptor.

Hydration, BodyMetrics and Cycles purge participants now use their owner contexts and rebind the live IModuleTransactionCoordinator.CurrentTransaction on every call. Existing orders 80/90/100, user predicates and Cycles child-first deletes are unchanged. Users still owns the encompassing transaction, final user-row deletion and receipt FK cascades. All three adapter projects no longer reference central Infrastructure; PostgreSQL tests cover rollback, scope reuse, cancellation, continuation after a failed user and survivor isolation.

Admin, Dietologist, Meals, MealPlanning, RecentItems, Ai and Identity purge participants also use owner contexts with live transaction binding on every invocation. Orders 30/40/50/60/70/120/130 and all user predicates remain unchanged; Meals children precede their parent and Ai jobs precede usage. Admin, Meals, MealPlanning and RecentItems no longer reference central Infrastructure. Dietologist retains its audit dependency, Ai the existing quota-orphan metric, and Identity shared JwtOptions. Shared PostgreSQL scenarios cover both actor/target and client/dietologist roles, nested cascades, rollback, scope reuse, cancellation, receipt lifecycle and continuation after a failed user.
