# RecentItems consumer contracts

Own only IRecentItemUsageReadService, IRecentItemUsageRecorder and their immutable usage records. Preserve namespaces, signatures, cancellation and post-commit semantics. Depend only on Products, Recipes and Users scalar Domain.Contracts. Repository interfaces and the post-commit implementation remain internal to the module; callers cannot save or transact through these contracts.
