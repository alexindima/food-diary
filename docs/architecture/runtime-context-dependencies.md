# Remaining shared runtime context dependencies

Source inventory after module context namespace normalization. Scope: production C# under `Modules/` containing the `FoodDiaryDbContext` identifier; test sources are excluded. This is not a list of every persistence capability: inferred DbSets, generic DbContext access and calls through other services are additionally reviewed in `persistence-capabilities.json` and its architecture tests.

All 29 runtime contexts use `FoodDiary.Modules.<Module>.Infrastructure.Persistence`. The ten normalized contexts keep their assembly names, mappings, tables and transaction behavior. The nine context files whose projects retain legacy roots have scoped IDE0130 exceptions because that rule infers the old project root; Admin has also normalized its project root and needs no exception. RuntimeContextNamespaceTests instead enforces their exact module ownership across all contexts. Project-wide legacy RootNamespace values remain for other implementation types; this step intentionally does not rename provider types or migration snapshots.

## What remains

| Category | Files |
|---|---:|
| Audit bridge | 1 |
| Connection/lock | 1 |
| Transaction/provider registration | 8 |
| Transaction coordination | 0 |
| User purge | 14 |

The presence of 29 owner contexts does not mean all runtime access has left the shared context. Dashboard body reads now live in host ReadModel.Composition. Image reassignment now uses ImagesDbContext on the caller connection. The three module replay streams also use owner contexts; their coordinator saves through IUnitOfWork. Audit retains reviewed shared-context behavior.

## Next changes, in order

1. All 29 registrations now use IModuleContextFactory for owner creation. Shared/FoodDiary.Persistence.Abstractions has no project dependencies; FoodDiaryDbContext implements it with the unchanged scoped provider/connection and tracker registration.
2. Extract transaction/reset/connection coordination separately after factory adoption; preserve current unit-of-work ownership.
3. Revisit purge and collaboration-audit bridges after transaction coordination is explicit. Preserve deletion order, FK behavior and audit atomicity.

The shared FoodDiaryDbContext partials and mapping composition remain necessary for the unified migration model and central read/purge integrations. Their removal is a separate architectural change, not part of a namespace rename.

## Complete direct-reference inventory

| Source | Category | Responsibility |
|---|---|---|
| [Modules/Admin/Infrastructure/Persistence/AdminUserDataPurgeParticipant.cs](../../Modules/Admin/Infrastructure/Persistence/AdminUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Ai/Infrastructure/ModuleRegistration.cs](../../Modules/Ai/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Ai/Infrastructure/Persistence/AiUserDataPurgeParticipant.cs](../../Modules/Ai/Infrastructure/Persistence/AiUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Billing/Infrastructure/Persistence/PostgresBillingCheckoutLock.cs](../../Modules/Billing/Infrastructure/Persistence/PostgresBillingCheckoutLock.cs) | Connection/lock | Obtains a connection string for a separate session advisory lease; no entity access. |
| [Modules/BodyMetrics/Infrastructure/Persistence/BodyMetricsUserDataPurgeParticipant.cs](../../Modules/BodyMetrics/Infrastructure/Persistence/BodyMetricsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Cycles/Infrastructure/Persistence/CyclesUserDataPurgeParticipant.cs](../../Modules/Cycles/Infrastructure/Persistence/CyclesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Dietologist/Infrastructure/Persistence/DietologistUserDataPurgeParticipant.cs](../../Modules/Dietologist/Infrastructure/Persistence/DietologistUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs](../../Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs) | Audit bridge | Inspects shared and Dietologist trackers, stages shared AuditEntry rows; preserve atomic save and event deduplication. |
| [Modules/Gamification/Infrastructure/ModuleRegistration.cs](../../Modules/Gamification/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Hydration/Infrastructure/Persistence/HydrationUserDataPurgeParticipant.cs](../../Modules/Hydration/Infrastructure/Persistence/HydrationUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Identity/Infrastructure/IdentityModuleRegistration.cs](../../Modules/Identity/Infrastructure/IdentityModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Identity/Infrastructure/Persistence/Authentication/IdentityUserDataPurgeParticipant.cs](../../Modules/Identity/Infrastructure/Persistence/Authentication/IdentityUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Images/Infrastructure/DependencyInjection.cs](../../Modules/Images/Infrastructure/DependencyInjection.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs](../../Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/MealPlanning/Infrastructure/Persistence/MealPlanningUserDataPurgeParticipant.cs](../../Modules/MealPlanning/Infrastructure/Persistence/MealPlanningUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs](../../Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Notifications/Infrastructure/ModuleRegistration.cs](../../Modules/Notifications/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs](../../Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs](../../Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/RecentItems/Infrastructure/ModuleRegistration.cs](../../Modules/RecentItems/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs](../../Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs](../../Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs](../../Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs) | User purge | User cleanup orchestration, user/role deletion, locks and profile-image unlinking. |
| [Modules/Users/Infrastructure/UsersModuleRegistration.cs](../../Modules/Users/Infrastructure/UsersModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |

WeeklyGoals no longer consumes FoodDiaryDbContext or central Infrastructure. Its live transaction accessor and advisory-lock runner use IModuleTransactionCoordinator; shared clean-entry/retry/reset/save/commit behavior remains in EfModuleTransactionCoordinator.

Meals recognition now also uses IModuleTransactionCoordinator. Its intermediate IUnitOfWork flush retains the owned Meal xmin and receipt atomicity. The Meals purge participant still consumes FoodDiaryDbContext.

Products and Recipes delegate Serializable mutations and live transaction access to IModuleTransactionCoordinator. Their ordered purge participants still retain central context access.

Billing delegates transaction/save/retry/reset and live transaction access to IModuleTransactionCoordinator; duplicate translation remains owner-side before cleanup. Its separate checkout-session advisory lease still uses the central connection.

Wearables no longer references FoodDiaryDbContext or central Infrastructure. IModuleSessionCoordinator owns its separate session advisory lease, single callback execution and clean/reset/final-save boundary. Intermediate saves remain durable and provider calls are never replayed.
