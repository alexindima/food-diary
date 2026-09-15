# Products Contracts

Own ProductErrors with unchanged error codes, messages and kinds; consumers must
not reference internal Abstractions merely for error factories.

Stable Product lookup/overview projections and semantic USDA linking capability consumed by other modules. No infrastructure or application implementation dependencies, no aggregate-returning API.

IProductSnapshotReadService returns immutable batch scalar snapshots for related
Meals/Recipes hydration. It preserves missing-ID behavior and does not perform
visibility authorization; callers retain their existing aggregate access checks.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
