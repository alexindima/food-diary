# RecentItems consumer contracts

Own only ReadRecentProductsQuery, ReadRecentRecipesQuery, IRecentItemUsageRecorder and their immutable usage records. Preserve signatures, cancellation and post-commit semantics. Depend only on Products, Recipes and Users scalar Domain.Contracts. Repository interfaces and the post-commit implementation remain internal to the module; callers cannot save or transact through these contracts.

Current module convention: all projects use `FoodDiary.Modules.RecentItems.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
