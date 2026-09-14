# Remaining shared runtime context dependencies

Source inventory after module context namespace normalization. Scope: production C# under `Modules/` containing the `FoodDiaryDbContext` identifier; test sources are excluded. This is not a list of every persistence capability: inferred DbSets, generic DbContext access and calls through other services are additionally reviewed in `persistence-capabilities.json` and its architecture tests.

All 29 runtime contexts use `FoodDiary.Modules.<Module>.Infrastructure.Persistence`. The ten normalized contexts keep their assembly names, mappings, tables and transaction behavior. The nine context files whose projects retain legacy roots have scoped IDE0130 exceptions because that rule infers the old project root; Admin has also normalized its project root and needs no exception. RuntimeContextNamespaceTests instead enforces their exact module ownership across all contexts. Project-wide legacy RootNamespace values remain for other implementation types; this step intentionally does not rename provider types or migration snapshots.

## What remains

| Category | Files |
|---|---:|
| Audit bridge | 1 |
| Connection/lock | 1 |
| Context registration | 29 |
| Outbox replay | 3 |
| Transaction coordination | 6 |
| User purge | 14 |

The presence of 29 owner contexts does not mean all runtime access has left the shared context. Dashboard body reads now live in host ReadModel.Composition. Image reassignment now uses ImagesDbContext on the caller connection. Outbox replay and audit retain reviewed shared-context behavior.

## Next changes, in order

1. Review the three outbox replay streams together with the shared replay coordinator. Preserve row locks, tracked entry mutation, scope reset and all-or-nothing replay saves; switching the constructor alone is insufficient.
2. Extract a narrow shared persistence coordination seam for context creation, transactions, reset and connection access. Central Infrastructure must not become a dependency of that seam.
3. Revisit purge and collaboration-audit bridges after transaction coordination is explicit. Preserve deletion order, FK behavior and audit atomicity.

The shared FoodDiaryDbContext partials and mapping composition remain necessary for the unified migration model and central read/purge integrations. Their removal is a separate architectural change, not part of a namespace rename.

## Complete direct-reference inventory

| Source | Category | Responsibility |
|---|---|---|
| [Modules/Admin/Infrastructure/AdminModuleRegistration.cs](../../Modules/Admin/Infrastructure/AdminModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Admin/Infrastructure/Persistence/AdminUserDataPurgeParticipant.cs](../../Modules/Admin/Infrastructure/Persistence/AdminUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Ai/Infrastructure/ModuleRegistration.cs](../../Modules/Ai/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Ai/Infrastructure/Persistence/AiUserDataPurgeParticipant.cs](../../Modules/Ai/Infrastructure/Persistence/AiUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Billing/Infrastructure/ModuleRegistration.cs](../../Modules/Billing/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Billing/Infrastructure/Persistence/EfBillingTransactionRunner.cs](../../Modules/Billing/Infrastructure/Persistence/EfBillingTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Billing/Infrastructure/Persistence/PostgresBillingCheckoutLock.cs](../../Modules/Billing/Infrastructure/Persistence/PostgresBillingCheckoutLock.cs) | Connection/lock | Obtains a connection string for a separate session advisory lease; no entity access. |
| [Modules/BodyMetrics/Infrastructure/ModuleRegistration.cs](../../Modules/BodyMetrics/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/BodyMetrics/Infrastructure/Persistence/BodyMetricsUserDataPurgeParticipant.cs](../../Modules/BodyMetrics/Infrastructure/Persistence/BodyMetricsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/ContentReports/Infrastructure/ModuleRegistration.cs](../../Modules/ContentReports/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Cycles/Infrastructure/ModuleRegistration.cs](../../Modules/Cycles/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Cycles/Infrastructure/Persistence/CyclesUserDataPurgeParticipant.cs](../../Modules/Cycles/Infrastructure/Persistence/CyclesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/DailyAdvices/Infrastructure/ModuleRegistration.cs](../../Modules/DailyAdvices/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Dietologist/Infrastructure/ModuleRegistration.cs](../../Modules/Dietologist/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Dietologist/Infrastructure/Persistence/DietologistUserDataPurgeParticipant.cs](../../Modules/Dietologist/Infrastructure/Persistence/DietologistUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs](../../Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs) | Audit bridge | Inspects shared and Dietologist trackers, stages shared AuditEntry rows; preserve atomic save and event deduplication. |
| [Modules/Exercises/Infrastructure/ModuleRegistration.cs](../../Modules/Exercises/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Fasting/Infrastructure/ModuleRegistration.cs](../../Modules/Fasting/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Favorites/Infrastructure/ModuleRegistration.cs](../../Modules/Favorites/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Gamification/Infrastructure/ModuleRegistration.cs](../../Modules/Gamification/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Gamification/Infrastructure/Persistence/AchievementEvaluationOutboxReplayStream.cs](../../Modules/Gamification/Infrastructure/Persistence/AchievementEvaluationOutboxReplayStream.cs) | Outbox replay | Tracked owner outbox lookup with optional FOR UPDATE and no-tracking dead-letter listing; caller coordinates save. |
| [Modules/Hydration/Infrastructure/ModuleRegistration.cs](../../Modules/Hydration/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Hydration/Infrastructure/Persistence/HydrationUserDataPurgeParticipant.cs](../../Modules/Hydration/Infrastructure/Persistence/HydrationUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Identity/Infrastructure/IdentityModuleRegistration.cs](../../Modules/Identity/Infrastructure/IdentityModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Identity/Infrastructure/Persistence/Authentication/IdentityUserDataPurgeParticipant.cs](../../Modules/Identity/Infrastructure/Persistence/Authentication/IdentityUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Images/Infrastructure/DependencyInjection.cs](../../Modules/Images/Infrastructure/DependencyInjection.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs](../../Modules/Images/Infrastructure/Persistence/ImagesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Images/Infrastructure/Persistence/Images/ImageDeletionOutboxReplayStream.cs](../../Modules/Images/Infrastructure/Persistence/Images/ImageDeletionOutboxReplayStream.cs) | Outbox replay | Tracked owner outbox lookup with optional FOR UPDATE and no-tracking dead-letter listing; caller coordinates save. |
| [Modules/Lessons/Infrastructure/ModuleRegistration.cs](../../Modules/Lessons/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Marketing/Infrastructure/ModuleRegistration.cs](../../Modules/Marketing/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/MealPlanning/Infrastructure/ModuleRegistration.cs](../../Modules/MealPlanning/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/MealPlanning/Infrastructure/Persistence/MealPlanningUserDataPurgeParticipant.cs](../../Modules/MealPlanning/Infrastructure/Persistence/MealPlanningUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Meals/Infrastructure/MealsModuleRegistration.cs](../../Modules/Meals/Infrastructure/MealsModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs](../../Modules/Meals/Infrastructure/Persistence/MealsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Meals/Infrastructure/Persistence/Meals/EfMealRecognitionTransactionRunner.cs](../../Modules/Meals/Infrastructure/Persistence/Meals/EfMealRecognitionTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Notifications/Infrastructure/ModuleRegistration.cs](../../Modules/Notifications/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Notifications/Infrastructure/Persistence/WebPushOutboxReplayStream.cs](../../Modules/Notifications/Infrastructure/Persistence/WebPushOutboxReplayStream.cs) | Outbox replay | Tracked owner outbox lookup with optional FOR UPDATE and no-tracking dead-letter listing; caller coordinates save. |
| [Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs](../../Modules/OpenFoodFacts/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs](../../Modules/Products/Infrastructure/Persistence/ProductsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Products/Infrastructure/Persistence/Products/EfProductMutationTransactionRunner.cs](../../Modules/Products/Infrastructure/Persistence/Products/EfProductMutationTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Products/Infrastructure/ProductsModuleRegistration.cs](../../Modules/Products/Infrastructure/ProductsModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/RecentItems/Infrastructure/ModuleRegistration.cs](../../Modules/RecentItems/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs](../../Modules/RecentItems/Infrastructure/Persistence/RecentItemsUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/RecipeCommunity/Infrastructure/ModuleRegistration.cs](../../Modules/RecipeCommunity/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs](../../Modules/Recipes/Infrastructure/Persistence/RecipesUserDataPurgeParticipant.cs) | User purge | Ordered bulk cleanup in the caller transaction; some participants also reassign retained content. |
| [Modules/Recipes/Infrastructure/Persistence/Recipes/EfRecipeMutationTransactionRunner.cs](../../Modules/Recipes/Infrastructure/Persistence/Recipes/EfRecipeMutationTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/Recipes/Infrastructure/RecipesModuleRegistration.cs](../../Modules/Recipes/Infrastructure/RecipesModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Usda/Infrastructure/ModuleRegistration.cs](../../Modules/Usda/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs](../../Modules/Users/Infrastructure/Persistence/Users/UserCleanupService.cs) | User purge | User cleanup orchestration, user/role deletion, locks and profile-image unlinking. |
| [Modules/Users/Infrastructure/UsersModuleRegistration.cs](../../Modules/Users/Infrastructure/UsersModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Wearables/Infrastructure/ModuleRegistration.cs](../../Modules/Wearables/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/Wearables/Infrastructure/Persistence/EfWearableTransactionRunner.cs](../../Modules/Wearables/Infrastructure/Persistence/EfWearableTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
| [Modules/WeeklyGoals/Infrastructure/ModuleRegistration.cs](../../Modules/WeeklyGoals/Infrastructure/ModuleRegistration.cs) | Context registration | Shared connection/options, owner context creation and live transaction synchronization. |
| [Modules/WeeklyGoals/Infrastructure/Persistence/EfWeeklyGoalTransactionRunner.cs](../../Modules/WeeklyGoals/Infrastructure/Persistence/EfWeeklyGoalTransactionRunner.cs) | Transaction coordination | Existing transaction/retry/reset boundary; owner writes are coordinated through the shared unit of work. |
