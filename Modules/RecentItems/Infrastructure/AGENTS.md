# RecentItems Infrastructure

Own repository, post-commit recorder and `AddRecentItemsModule`. Preserve queue-after-commit behavior and the conditional shared-unit-of-work save. The shared context remains central.

RecentItemsDbContext owns runtime recency tracking and SQL. AddRecentItemsModule
shares the central connection through IModuleContextFactory and supplies a live
IModuleTransactionCoordinator.CurrentTransaction accessor. Do not capture the
transaction at repository resolution or resolve FoodDiaryDbContext in registration. Before
relational reads or usage upserts, synchronize that transaction, including null
after commit/rollback. Keep SQL conflict handling, timestamp monotonicity,
saturating counters and 100-per-type retention unchanged. The InMemory fallback
tracks only owned rows and uses the shared UnitOfWork in post-commit callbacks.
User purge retains its immediate central-context deletion in the caller transaction.
