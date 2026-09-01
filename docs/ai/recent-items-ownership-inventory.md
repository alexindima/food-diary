# RecentItems ownership inventory

`Modules/RecentItems` physically owns the `RecentItem` aggregate, enum and ID; narrow repository/usage contracts; repository and post-commit recorder; DI; and EF configuration. Existing CLR namespaces are preserved.

The central `FoodDiaryDbContext`, migrations and snapshot remain the database authority and explicitly apply `ApplyRecentItemsPersistenceModel`. `User` and `UserId` remain central; `RecentItem.User` is a one-way dependency and no inverse navigation is introduced. `UserCleanupService` remains Users-owned.

Products and Recipes use `IRecentItemUsageReadService`; Meals uses `IRecentItemUsageRecorder`. Hosts compose `AddRecentItemsModule`. The recorder still enqueues work after the owning transaction commits and saves only when the shared unit of work reports pending changes. The extraction has no EF model or HTTP contract delta, so it adds no migration or API snapshot.
