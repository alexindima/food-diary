# Remaining shared runtime context dependencies

Source inventory after module context namespace normalization. Scope: production C# under `Modules/` containing the `FoodDiaryDbContext` identifier; test sources are excluded. This is not a list of every persistence capability: inferred DbSets, generic DbContext access and calls through other services are additionally reviewed in `persistence-capabilities.json` and its architecture tests.

All 29 runtime contexts use `FoodDiary.Modules.<Module>.Infrastructure.Persistence`. The ten normalized contexts keep their assembly names, mappings, tables and transaction behavior. The nine context files whose projects retain legacy roots have scoped IDE0130 exceptions because that rule infers the old project root; Admin has also normalized its project root and needs no exception. RuntimeContextNamespaceTests instead enforces their exact module ownership across all contexts. Project-wide legacy RootNamespace values remain for other implementation types; this step intentionally does not rename provider types or migration snapshots.

## What remains

| Category | Files |
|---|---:|
| Audit bridge | 1 |
| Connection/lock | 1 |
| Transaction/provider registration | 13 |
| Transaction coordination | 6 |
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
| [Modules/Billing/Infrastructure/ModuleRegistration.cs](../../Modules/Billing/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Billing/Infrastructure/Persistence/EfBillingTransactionRunner.cs](../../Modules/Billing/Infrastructure/Persistence/EfBillingTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
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
| [Modules/Meals/Infrastructure/MealsModuleRegistration.cs](../../Modules/Meals/Infrastructure/MealsModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs](../../Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Meals/Infrastructure/Persistence/Meals/EfMealRecognitionTransactionRunner.cs](../../Modules/Meals/Infrastructure/Persistence/Meals/EfMealRecognitionTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Notifications/Infrastructure/ModuleRegistration.cs](../../Modules/Notifications/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs](../../Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs](../../Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Products/Infrastructure/Persistence/Products/EfProductMutationTransactionRunner.cs](../../Modules/Products/Infrastructure/Persistence/Products/EfProductMutationTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Products/Infrastructure/ProductsModuleRegistration.cs](../../Modules/Products/Infrastructure/ProductsModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/RecentItems/Infrastructure/ModuleRegistration.cs](../../Modules/RecentItems/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs](../../Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs](../../Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Recipes/Infrastructure/Persistence/Recipes/EfRecipeMutationTransactionRunner.cs](../../Modules/Recipes/Infrastructure/Persistence/Recipes/EfRecipeMutationTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Recipes/Infrastructure/RecipesModuleRegistration.cs](../../Modules/Recipes/Infrastructure/RecipesModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs](../../Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs) | User purge | User cleanup orchestration, user/role deletion, locks and profile-image unlinking. |
| [Modules/Users/Infrastructure/UsersModuleRegistration.cs](../../Modules/Users/Infrastructure/UsersModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/Wearables/Infrastructure/Persistence/EfWearableTransactionRunner.cs](../../Modules/Wearables/Infrastructure/Persistence/EfWearableTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/WeeklyGoals/Infrastructure/ModuleRegistration.cs](../../Modules/WeeklyGoals/Infrastructure/ModuleRegistration.cs) | Transaction/provider registration | Existing transaction synchronizers, clean-entry callbacks or independent provider options; owner creation uses IModuleContextFactory. |
| [Modules/WeeklyGoals/Infrastructure/Persistence/EfWeeklyGoalTransactionRunner.cs](../../Modules/WeeklyGoals/Infrastructure/Persistence/EfWeeklyGoalTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
