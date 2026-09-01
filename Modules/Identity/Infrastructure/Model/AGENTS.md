# Identity persistence model

Own EF configurations for Identity-owned domain entities. Preserve table, column, index, foreign-key, conversion, concurrency, and delete semantics. Central `FoodDiaryDbContext`, migrations, and model snapshot remain central and call `ApplyIdentityPersistenceModel` explicitly.

Do not add a migration when `has-pending-model-changes` reports no delta.
